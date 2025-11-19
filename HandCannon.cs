using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hand Cannon", "handcaster", "1.0.0")]
    [Description("Adds a Hand Cannon weapon that fires high velocity rockets")]
    public class HandCannon : RustPlugin
    {
        private List<ulong> ActiveHandCannons = new List<ulong>();
        private ulong handCannonSkinId = 0; // Use default skin first to test
        
        #region Configuration
        private Configuration config;

        public class Configuration
        {
            public string HandCannonSkinId = "0"; // Default skin
            public string HandCannonDisplayName = "Hand Cannon";
            public int MaxAmmoCapacity = 6;
            public string AmmoType = "ammo.rocket.hv";
            public float ProjectileSpeed = 200f;
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
            if (!ulong.TryParse(config.HandCannonSkinId, out handCannonSkinId))
            {
                PrintError($"Invalid skin ID: {config.HandCannonSkinId}. Using default.");
                handCannonSkinId = 0;
            }
            
            PrintWarning($"HandCannon skin ID set to: {handCannonSkinId}");
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Hooks
        void Init()
        {
            permission.RegisterPermission("handcannon.give", this);
        }

        void OnServerInitialized()
        {
            PrintWarning("Hand Cannon plugin loaded! Use /givehc to get one.");
        }

        // Use pistol ammo detection since that's what works
        void OnWeaponFired(BaseProjectile weapon, BasePlayer player, ItemModProjectile ammo, ProtoBuf.ProjectileShoot projectiles)
        {
            if (player == null) return;
            
            if (IsHandCannon(weapon.GetItem()) && ammo.ammoType == Rust.AmmoTypes.PISTOL_9MM)
            {
                PrintWarning($"HandCannon fired by {player.displayName} with ammo type: {ammo.name}");
                
                string rocket = "assets/prefabs/ammo/rocket/rocket_hv.prefab";
                FireRockets(player, rocket);
            }
        }

        // Initialize the weapon properly when equipped
        void OnPlayerActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null && IsHandCannon(newItem))
            {
                var weapon = newItem.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    // Initialize with proper ammo type to enable reload animations
                    InitializeHandCannonAmmo(weapon);
                }
            }
        }

        // Intercept unload to give back HV rockets instead of pistol ammo
        void OnAmmoUnload(BaseProjectile weapon, Item item, BasePlayer player)
        {
            if (IsHandCannon(weapon.GetItem()))
            {
                timer.Once(0.1f, () => {
                    // Remove any pistol ammo that was unloaded
                    var pistolAmmo = player.inventory.FindItemByItemID(-1211166256); // pistol ammo ID
                    if (pistolAmmo != null)
                    {
                        int ammoCount = pistolAmmo.amount;
                        pistolAmmo.Remove();
                        
                        // Give HV rockets instead
                        var hvRockets = ItemManager.Create(ItemManager.FindItemDefinition(config.AmmoType), ammoCount);
                        if (!hvRockets.MoveToContainer(player.inventory.containerMain))
                        {
                            hvRockets.Drop(player.transform.position, Vector3.zero);
                        }
                        player.ChatMessage($"Unloaded {ammoCount} HV rockets from Hand Cannon");
                    }
                });
            }
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player.GetActiveItem() != null && IsHandCannon(player.GetActiveItem()))
            {
                if (input.WasJustPressed(BUTTON.RELOAD))
                {
                    var weapon = player.GetActiveItem().GetHeldEntity() as BaseProjectile;
                    if (weapon != null)
                    {
                        LoadHandCannonAmmo(weapon, player);
                    }
                }
            }
        }
        #endregion

        #region Core Methods
        private bool IsHandCannon(Item item)
        {
            if (item?.info?.shortname != "pistol.python") return false;
            return item.name == config.HandCannonDisplayName;
        }

        private void InitializeHandCannonAmmo(BaseProjectile weapon)
        {
            if (weapon == null) return;
            
            // Set the ammo type to pistol definition to enable proper reload animations
            var pistolAmmoDefinition = ItemManager.FindItemDefinition("ammo.pistol");
            if (pistolAmmoDefinition != null)
            {
                weapon.primaryMagazine.ammoType = pistolAmmoDefinition;
                weapon.SendNetworkUpdateImmediate();
            }
        }

        public void FireRockets(BasePlayer player, string rocketPrefab)
        {
            if (player == null) return;
            
            PrintWarning($"Attempting to fire rocket for {player.displayName}");
            
            var rocket = GameManager.server.CreateEntity(rocketPrefab, player.eyes.position, new Quaternion());
            if (rocket != null)
            {
                PrintWarning($"Rocket entity created: {rocket.ShortPrefabName}");
                
                if(rocketPrefab != null)
                {
                    rocket.creatorEntity = player;
                    rocket.SendMessage("InitializeVelocity", player.eyes.HeadForward() * config.ProjectileSpeed);
                    rocket.OwnerID = player.userID;
                    rocket.Spawn();
                    rocket.ClientRPC(null, "RPCFire");
                    
                    PrintWarning($"Rocket spawned and velocity set");
                    
                    Interface.CallHook("OnRocketLaunched", player, rocket);
                }
            }
            else
            {
                PrintError("Failed to create rocket entity!");
            }
        }

        private void LoadHandCannonAmmo(BaseProjectile weapon, BasePlayer player)
        {
            try
            {
                var item = weapon.GetItem();
                if (!IsHandCannon(item)) return;

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
                        
                        // Add some pistol ammo temporarily to trigger the reload animation
                        var tempPistolAmmo = ItemManager.Create(ItemManager.FindItemDefinition("ammo.pistol"), 1);
                        if (tempPistolAmmo != null)
                        {
                            // Put pistol ammo in player inventory temporarily
                            tempPistolAmmo.MoveToContainer(player.inventory.containerMain);
                            
                            // Set magazine to empty and let the game naturally reload
                            weapon.primaryMagazine.contents = 0;
                            
                            // Add delay to allow reload animation, then fix the ammo
                            timer.Once(1.0f, () => {
                                if (weapon != null && !weapon.IsDestroyed)
                                {
                                    // Remove the temporary pistol ammo
                                    var foundPistolAmmo = player.inventory.FindItemByItemID(-1211166256);
                                    if (foundPistolAmmo != null)
                                    {
                                        foundPistolAmmo.Remove();
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
                PrintError($"Error in LoadHandCannonAmmo: {ex.Message}");
            }
        }
        #endregion

        #region Commands
        [ChatCommand("givehc")]
        private void GiveHandCannonCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "handcannon.give"))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            // Create Python without skin first, then apply it manually
            var pythonItem = ItemManager.CreateByName("pistol.python", 1);
            if (pythonItem != null)
            {
                // Set name to identify it as HandCannon
                pythonItem.name = config.HandCannonDisplayName;
                
                var weapon = pythonItem.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    weapon.primaryMagazine.contents = config.MaxAmmoCapacity;
                    weapon.primaryMagazine.capacity = config.MaxAmmoCapacity;
                    
                    // Initialize with proper ammo type for reload animations
                    var pistolAmmoDefinition = ItemManager.FindItemDefinition("ammo.pistol");
                    if (pistolAmmoDefinition != null)
                    {
                        weapon.primaryMagazine.ammoType = pistolAmmoDefinition;
                    }
                }

                // Remove attachment slots
                if (pythonItem.contents != null)
                {
                    for (int i = pythonItem.contents.itemList.Count - 1; i >= 0; i--)
                    {
                        var attachment = pythonItem.contents.itemList[i];
                        attachment.Remove();
                    }
                    pythonItem.contents.capacity = 0;
                }

                pythonItem.MarkDirty();
                
                if (!pythonItem.MoveToContainer(player.inventory.containerMain))
                {
                    pythonItem.Drop(player.transform.position, Vector3.zero);
                }

                // Give HV rockets
                var ammo = ItemManager.Create(ItemManager.FindItemDefinition(config.AmmoType), 30);
                if (!ammo.MoveToContainer(player.inventory.containerMain))
                {
                    ammo.Drop(player.transform.position, Vector3.zero);
                }
            }
        }

        [ChatCommand("hcskin")]
        private void SetHandCannonSkinCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "handcannon.give"))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage("Usage: /hcskin <skinid> - Apply skin to HandCannon in your hands");
                return;
            }

            if (!ulong.TryParse(args[0], out ulong skinId))
            {
                player.ChatMessage("Invalid skin ID. Please provide a numeric skin ID.");
                return;
            }

            var activeItem = player.GetActiveItem();
            if (activeItem == null || !IsHandCannon(activeItem))
            {
                player.ChatMessage("You must be holding a HandCannon to apply a skin.");
                return;
            }

            activeItem.skin = skinId;
            activeItem.MarkDirty();
            
            player.ChatMessage($"Applied skin ID {skinId} to your HandCannon!");
        }
        #endregion
    }
}