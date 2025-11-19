using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MiniGun Rockets", "handcaster", "1.0.0")]
    [Description("Adds a MiniGun weapon that fires high velocity rockets")]
    public class MiniDeath : RustPlugin
    {
        private List<ulong> ActiveMiniDeaths = new List<ulong>();
        private ulong miniDeathSkinId = 0;
        private List<ItemId> pendingMiniDeaths = new List<ItemId>(); // Track items waiting for entity spawn
        
        #region Configuration
        private Configuration config;

        public class Configuration
        {
            public string MiniDeathSkinId = "0"; // Default skin
            public string MiniDeathDisplayName = "Mini Death";
            public int MaxAmmoCapacity = 300;
            public string AmmoType = "ammo.rocket.hv";
            public float ProjectileSpeed = 300f; // Speed of the rockets adjusted from 200f to 300f
            public float DamageMultiplier = 8.0f; // Octuple damage
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            
            // Parse the skin ID once when config loads
            if (!ulong.TryParse(config.MiniDeathSkinId, out miniDeathSkinId))
            {
                PrintError($"Invalid skin ID: {config.MiniDeathSkinId}. Using default.");
                miniDeathSkinId = 0;
            }
            
            PrintWarning($"MiniDeath skin ID set to: {miniDeathSkinId}");
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Hooks
        void Init()
        {
            permission.RegisterPermission("minideath.give", this);
        }

        void OnServerInitialized()
        {
            PrintWarning("Mini Death plugin loaded! Use /givemd to get one.");
        }

        // Hook when weapon entities are spawned and modify MiniDeath weapons
        void OnEntitySpawned(BaseProjectile weapon)
        {
            if (weapon == null) return;
            
            var item = weapon.GetItem();
            if (item != null && IsMiniDeath(item))
            {
                // This is a MiniDeath weapon being spawned, modify it now
                weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                weapon.primaryMagazine.contents = config.MaxAmmoCapacity;
                
                var rifleAmmoDefinition = ItemManager.FindItemDefinition("ammo.rifle");
                if (rifleAmmoDefinition != null)
                {
                    weapon.primaryMagazine.ammoType = rifleAmmoDefinition;
                }
                
                weapon.SendNetworkUpdateImmediate();
                
                // Force another update to ensure it sticks
                NextTick(() => {
                    if (weapon != null && !weapon.IsDestroyed)
                    {
                        weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                        weapon.primaryMagazine.contents = config.MaxAmmoCapacity;
                        weapon.SendNetworkUpdateImmediate();
                    }
                });
            }
            // Also check if this is a pending MiniDeath by item ID
            else if (item != null && pendingMiniDeaths.Contains(item.uid))
            {
                weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                weapon.primaryMagazine.contents = config.MaxAmmoCapacity;
                
                var rifleAmmoDefinition = ItemManager.FindItemDefinition("ammo.rifle");
                if (rifleAmmoDefinition != null)
                {
                    weapon.primaryMagazine.ammoType = rifleAmmoDefinition;
                }
                
                weapon.SendNetworkUpdateImmediate();
                pendingMiniDeaths.Remove(item.uid);
                
                // Force another update to ensure it sticks
                NextTick(() => {
                    if (weapon != null && !weapon.IsDestroyed)
                    {
                        weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                        weapon.primaryMagazine.contents = config.MaxAmmoCapacity;
                        weapon.SendNetworkUpdateImmediate();
                    }
                });
            }
        }

        // Hook for when rockets spawn to modify their damage
        void OnEntitySpawned(TimedExplosive rocket)
        {
            if (rocket == null || rocket.ShortPrefabName != "rocket_hv") return;
            
            // Check if this rocket was fired from a MiniDeath
            if (rocket.creatorEntity is BasePlayer player)
            {
                var activeItem = player.GetActiveItem();
                if (activeItem != null && IsMiniDeath(activeItem))
                {
                    // This rocket came from a MiniDeath, modify its damage
                    var explosive = rocket.GetComponent<TimedExplosive>();
                    if (explosive != null)
                    {
                        explosive.explosionRadius *= config.DamageMultiplier;
                        explosive.timerAmountMax *= config.DamageMultiplier;
                        explosive.timerAmountMin *= config.DamageMultiplier;
                    }
                    
                    // Also modify the damage list
                    if (rocket.damageTypes != null)
                    {
                        for (int i = 0; i < rocket.damageTypes.Count; i++)
                        {
                            rocket.damageTypes[i].amount *= config.DamageMultiplier;
                        }
                    }
                }
            }
        }

        // Use 5.56 ammo detection since that's what the minigun uses
        void OnWeaponFired(BaseProjectile weapon, BasePlayer player, ItemModProjectile ammo, ProtoBuf.ProjectileShoot projectiles)
        {
            if (player == null) return;
            
            if (IsMiniDeath(weapon.GetItem()) && ammo.ammoType == Rust.AmmoTypes.RIFLE_556MM)
            {
                string rocket = "assets/prefabs/ammo/rocket/rocket_hv.prefab";
                FireRockets(player, rocket);
            }
        }

        // Initialize the weapon properly when equipped
        void OnPlayerActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null && IsMiniDeath(newItem))
            {
                var weapon = newItem.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    // Initialize with proper ammo type to enable reload animations
                    InitializeMiniDeathAmmo(weapon);
                    
                    // Force the capacity after equipping (backup)
                    weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                    weapon.SendNetworkUpdateImmediate();
                    
                    // Double check with delay
                    timer.Once(0.1f, () => {
                        if (weapon != null && !weapon.IsDestroyed)
                        {
                            weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                            weapon.SendNetworkUpdateImmediate();
                        }
                    });
                }
            }
        }

        // Intercept unload to give back HV rockets instead of 5.56 ammo
        void OnAmmoUnload(BaseProjectile weapon, Item item, BasePlayer player)
        {
            if (IsMiniDeath(weapon.GetItem()))
            {
                timer.Once(0.1f, () => {
                    // Remove any 5.56 ammo that was unloaded
                    var rifleAmmo = player.inventory.FindItemByItemID(-1211166256); // 5.56 ammo ID
                    if (rifleAmmo != null)
                    {
                        int ammoCount = rifleAmmo.amount;
                        rifleAmmo.Remove();
                        
                        // Give HV rockets instead
                        var hvRockets = ItemManager.Create(ItemManager.FindItemDefinition(config.AmmoType), ammoCount);
                        if (!hvRockets.MoveToContainer(player.inventory.containerMain))
                        {
                            hvRockets.Drop(player.transform.position, Vector3.zero);
                        }
                        player.ChatMessage($"Unloaded {ammoCount} HV rockets from Mini Death");
                    }
                });
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player.GetActiveItem() != null && IsMiniDeath(player.GetActiveItem()))
            {
                if (input.WasJustPressed(BUTTON.RELOAD))
                {
                    var weapon = player.GetActiveItem().GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        LoadMiniDeathAmmo(weapon, player);
                    }
                }
            }
        }
        #endregion

        #region Core Methods
        private bool IsMiniDeath(Item item)
        {
            if (item?.info?.shortname != "minigun") return false;
            return item.name == config.MiniDeathDisplayName;
        }

        private void InitializeMiniDeathAmmo(BaseProjectile weapon)
        {
            if (weapon == null) return;
            
            // Set the ammo type to 5.56 definition to enable reload animations
            var rifleAmmoDefinition = ItemManager.FindItemDefinition("ammo.rifle");
            if (rifleAmmoDefinition != null)
            {
                weapon.primaryMagazine.ammoType = rifleAmmoDefinition;
                weapon.SendNetworkUpdateImmediate();
            }
        }

        public void FireRockets(BasePlayer player, string rocketPrefab)
        {
            if (player == null) return;
            
            var rocket = GameManager.server.CreateEntity(rocketPrefab, player.eyes.position, new Quaternion());
            if (rocket != null)
            {
                rocket.creatorEntity = player;
                rocket.SendMessage("InitializeVelocity", player.eyes.HeadForward() * config.ProjectileSpeed);
                rocket.OwnerID = player.userID;
                rocket.Spawn();
                rocket.ClientRPC(null, "RPCFire");
                
                Interface.CallHook("OnRocketLaunched", player, rocket);
            }
        }

        private void LoadMiniDeathAmmo(BaseProjectile weapon, BasePlayer player)
        {
            try
            {
                var item = weapon.GetItem();
                if (!IsMiniDeath(item)) return;

                Item hvRocketItem = null;
                
                // Look for HV rockets in inventory
                foreach (var invItem in player.inventory.containerMain.itemList)
                {
                    if (invItem.info.shortname == config.AmmoType)
                    {
                        hvRocketItem = invItem;
                        break;
                    }
                }
                
                if (hvRocketItem == null)
                {
                    foreach (var invItem in player.inventory.containerBelt.itemList)
                    {
                        if (invItem.info.shortname == config.AmmoType)
                        {
                            hvRocketItem = invItem;
                            break;
                        }
                    }
                }

                if (hvRocketItem != null)
                {
                    int currentAmmo = weapon.primaryMagazine.contents;
                    int maxCapacity = weapon.primaryMagazine.capacity;
                    int neededAmmo = maxCapacity - currentAmmo;
                    
                    if (neededAmmo > 0)
                    {
                        int ammoToTake = Mathf.Min(neededAmmo, hvRocketItem.amount);
                        
                        // Remove HV rockets from inventory
                        hvRocketItem.UseItem(ammoToTake);
                        
                        // Add some 5.56 ammo temporarily to trigger the reload animation
                        var tempRifleAmmo = ItemManager.Create(ItemManager.FindItemDefinition("ammo.rifle"), 1);
                        if (tempRifleAmmo != null)
                        {
                            // Put 5.56 ammo in player inventory temporarily
                            tempRifleAmmo.MoveToContainer(player.inventory.containerMain);
                            
                            // Set magazine to empty and let the game naturally reload
                            weapon.primaryMagazine.contents = 0;
                            
                            // Add delay to allow reload animation, then fix the ammo
                            timer.Once(1.0f, () => {
                                if (weapon != null && !weapon.IsDestroyed)
                                {
                                    // Remove the temporary 5.56 ammo
                                    var foundRifleAmmo = player.inventory.FindItemByItemID(1712070256); // 5.56 ammo ID
                                    if (foundRifleAmmo != null)
                                    {
                                        foundRifleAmmo.Remove();
                                    }
                                    
                                    // Set correct ammo count
                                    weapon.primaryMagazine.contents = currentAmmo + ammoToTake;
                                    weapon.SendNetworkUpdateImmediate();
                                }
                            });
                        }
                        else
                        {
                            // Fallback if temp ammo fails - just add the ammo directly
                            weapon.primaryMagazine.contents += ammoToTake;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                PrintError($"Error in LoadMiniDeathAmmo: {ex.Message}");
            }
        }
        #endregion

        #region Commands
        [ChatCommand("givemd")]
        private void GiveMiniDeathCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "minideath.give"))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            // Create Minigun
            var minigunItem = ItemManager.CreateByName("minigun", 1);
            if (minigunItem != null)
            {
                // Set name to identify it as MiniDeath
                minigunItem.name = config.MiniDeathDisplayName;
                
                // Add to pending list so OnEntitySpawned can catch it
                pendingMiniDeaths.Add(minigunItem.uid);

                // Remove attachment slots
                if (minigunItem.contents != null)
                {
                    for (int i = minigunItem.contents.itemList.Count - 1; i >= 0; i--)
                    {
                        var attachment = minigunItem.contents.itemList[i];
                        attachment.Remove();
                    }
                    minigunItem.contents.capacity = 0;
                }

                minigunItem.MarkDirty();
                
                // Give it to the player - this will trigger entity spawn
                player.GiveItem(minigunItem);
                
                // Force check the weapon after a short delay
                timer.Once(0.2f, () => {
                    var activeItem = player.GetActiveItem();
                    if (activeItem != null && IsMiniDeath(activeItem))
                    {
                        var weapon = activeItem.GetHeldEntity() as BaseProjectile;
                        if (weapon != null)
                        {
                            weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                            weapon.primaryMagazine.contents = config.MaxAmmoCapacity;
                            weapon.SendNetworkUpdateImmediate();
                        }
                    }
                });
            }
        }

        [ChatCommand("mdskin")]
        private void SetMiniDeathSkinCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "minideath.give"))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage("Usage: /mdskin <skinid> - Apply skin to MiniDeath in your hands");
                return;
            }

            if (!ulong.TryParse(args[0], out ulong skinId))
            {
                player.ChatMessage("Invalid skin ID. Please provide a numeric skin ID.");
                return;
            }

            var activeItem = player.GetActiveItem();
            if (activeItem == null || !IsMiniDeath(activeItem))
            {
                player.ChatMessage("You must be holding a MiniDeath to apply a skin.");
                return;
            }

            activeItem.skin = skinId;
            activeItem.MarkDirty();
            
            player.ChatMessage($"Applied skin ID {skinId} to your MiniDeath!");
        }
        #endregion
    }
}