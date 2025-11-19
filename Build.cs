using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Build", "handcaster", "1.1.0")]
    [Description("Opens a UI to give building materials to players")]
    public class Build : RustPlugin
    {
        private const string PermUse = "build.use";
        
        // Track players who have the UI open and their current tab
        private HashSet<ulong> playersWithUIOpen = new HashSet<ulong>();
        private Dictionary<ulong, string> playerCurrentTab = new Dictionary<ulong, string>();
        private Dictionary<ulong, int> playerGearPage = new Dictionary<ulong, int>();

        private void Init()
        {
            permission.RegisterPermission(PermUse, this);
            AddCovalenceCommand("build", "BuildCommand");
            AddCovalenceCommand("b", "BuildCommand"); // Add /b command
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "BuildUI");
            }
            playersWithUIOpen.Clear();
            playerCurrentTab.Clear();
            playerGearPage.Clear();
        }

        // Intercept ESC key before game menu opens
        private object CanOpenGameMenu(BasePlayer player)
        {
            if (player != null && playersWithUIOpen.Contains(player.userID))
            {
                // Close our UI instead of opening game menu
                CloseBuildUI(player);
                return false; // Prevent game menu from opening
            }
            return null; // Allow normal behavior
        }

        private void BuildCommand(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = (BasePlayer)iplayer.Object;
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse))
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }
            
            OpenBuildUI(player, "building");
        }

        private void OpenBuildUI(BasePlayer player, string tab = "building")
        {
            // Close existing UI first
            CuiHelper.DestroyUi(player, "BuildUI");

            // Add player to tracking set and set current tab
            playersWithUIOpen.Add(player.userID);
            playerCurrentTab[player.userID] = tab;
            
            // Initialize gear page if not set
            if (!playerGearPage.ContainsKey(player.userID))
                playerGearPage[player.userID] = 1;

            // Create main container
            CuiElementContainer container = UI.CreateElementContainer("Overlay", "BuildUI", "0.1 0.1 0.1 0.95", "0.35 0.15", "0.65 0.85", true);

            // Title
            UI.CreateLabel(ref container, "BuildUI", "1 1 1 1", "shift - ui v1.1", 18, "0 0.93", "1 0.98");

            // Create tab buttons
            CreateTabButton(ref container, "building", "building", tab == "building", "0.05 0.85", "0.32 0.92");
            CreateTabButton(ref container, "decorations", "decorations", tab == "decorations", "0.34 0.85", "0.66 0.92");
            CreateTabButton(ref container, "gear", "gear", tab == "gear", "0.68 0.85", "0.95 0.92");

            // Content area based on selected tab
            switch (tab)
            {
                case "building":
                    CreateBuildingContent(ref container);
                    break;
                case "decorations":
                    CreateDecorationsContent(ref container);
                    break;
                case "gear":
                    CreateGearContent(ref container, player);
                    break;
            }

            // Close button (lowercase)
            UI.CreateButton(ref container, "BuildUI", "0.8 0.2 0.2 1", "close", 14, "0.3 0.02", "0.7 0.08", "build.closeui");

            CuiHelper.AddUi(player, container);
        }

        private void CreateTabButton(ref CuiElementContainer container, string text, string tabName, bool isActive, string aMin, string aMax)
        {
            string color = isActive ? "0.3 0.5 0.7 1" : "0.2 0.2 0.2 0.8";
            UI.CreateButton(ref container, "BuildUI", color, text, 12, aMin, aMax, $"build.switchtab {tabName}");
        }

        private void CreateBuildingContent(ref CuiElementContainer container)
        {
            // Wood button
            CreateItemButton(ref container, "build.giveitem wood", "https://rustlabs.com/img/items180/wood.png", "wood (1000)", "0.05 0.72", "0.48 0.82");

            // Stone button
            CreateItemButton(ref container, "build.giveitem stones", "https://rustlabs.com/img/items180/stones.png", "stone (1000)", "0.52 0.72", "0.95 0.82");

            // Metal Fragments button
            CreateItemButton(ref container, "build.giveitem metal.fragments", "https://rustlabs.com/img/items180/metal.fragments.png", "metal frags (1000)", "0.05 0.59", "0.48 0.69");

            // High Quality Metal button
            CreateItemButton(ref container, "build.giveitem metal.refined", "https://rustlabs.com/img/items180/metal.refined.png", "hq (1000)", "0.52 0.59", "0.95 0.69");

            // Building Plan button
            CreateItemButton(ref container, "build.givetools building.planner", "https://rustlabs.com/img/items180/building.planner.png", "building plan", "0.05 0.46", "0.48 0.56");

            // Retro Tool Cupboard button
            CreateItemButton(ref container, "build.giveretrotc cupboard.tool.retro", "https://wiki.rustclash.com/img/skins/324/10238.png", "retro tc", "0.52 0.46", "0.95 0.56");

            // Hammer button
            CreateItemButton(ref container, "build.giveskinnedtools hammer", "https://wiki.rustclash.com/img/skins/324/44601.png", "hammer", "0.05 0.33", "0.48 0.43");

            // Toolgun button
            CreateItemButton(ref container, "build.givetools toolgun", "https://rustlabs.com/img/items180/toolgun.png", "garry's toolgun", "0.52 0.33", "0.95 0.43");

            // Spraycan button
            CreateItemButton(ref container, "build.givetools spraycan", "https://rustlabs.com/img/items180/spraycan.png", "spraycan", "0.05 0.20", "0.48 0.30");

            // Outfit button
            CreateItemButton(ref container, "build.giveoutfit outfit", "https://wiki.rustclash.com/img/skins/324/10086.png", "drip", "0.52 0.20", "0.95 0.30");
        }

        private void CreateDecorationsContent(ref CuiElementContainer container)
        {
            // Example decoration items - you can customize these
            CreateItemButton(ref container, "build.givetools lantern", "https://rustlabs.com/img/items180/lantern.png", "lantern", "0.05 0.72", "0.48 0.82");

            CreateItemButton(ref container, "build.givetools chair", "https://rustlabs.com/img/items180/chair.png", "chair", "0.52 0.72", "0.95 0.82");

            CreateItemButton(ref container, "build.givetools table", "https://rustlabs.com/img/items180/table.png", "table", "0.05 0.59", "0.48 0.69");

            CreateItemButton(ref container, "build.givetools rug", "https://rustlabs.com/img/items180/rug.png", "rug", "0.52 0.59", "0.95 0.69");

            CreateItemButton(ref container, "build.givetools painting", "https://rustlabs.com/img/items180/sign.pictureframe.landscape.png", "painting", "0.05 0.46", "0.48 0.56");

            CreateItemButton(ref container, "build.givetools campfire", "https://rustlabs.com/img/items180/campfire.png", "campfire", "0.52 0.46", "0.95 0.56");
        }

        private void CreateGearContent(ref CuiElementContainer container, BasePlayer player)
        {
            int currentPage = playerGearPage.ContainsKey(player.userID) ? playerGearPage[player.userID] : 1;

            // Page navigation buttons
            if (currentPage > 1)
            {
                UI.CreateButton(ref container, "BuildUI", "0.3 0.3 0.3 1", "< Prev", 10, "0.05 0.11", "0.2 0.17", $"build.gearpage {currentPage - 1}");
            }
            
            UI.CreateLabel(ref container, "BuildUI", "1 1 1 1", $"Page {currentPage}/4", 12, "0.22 0.11", "0.78 0.17");
            
            if (currentPage < 4)
            {
                UI.CreateButton(ref container, "BuildUI", "0.3 0.3 0.3 1", "Next >", 10, "0.8 0.11", "0.95 0.17", $"build.gearpage {currentPage + 1}");
            }

            switch (currentPage)
            {
                case 1: // Weapons & Armor
                    CreateWeaponsAndArmorPage(ref container);
                    break;
                case 2: // Explosives & Heavy Weapons
                    CreateExplosivesPage(ref container);
                    break;
                case 3: // Ammunition
                    CreateAmmunitionPage(ref container);
                    break;
                case 4: // Attachments
                    CreateAttachmentsPage(ref container);
                    break;
            }
        }

        private void CreateWeaponsAndArmorPage(ref CuiElementContainer container)
        {
            CreateItemButton(ref container, "build.givetools rifle.ak", "https://rustlabs.com/img/items180/rifle.ak.png", "ak-47", "0.05 0.72", "0.32 0.82");
            CreateItemButton(ref container, "build.givetools rifle.lr300", "https://rustlabs.com/img/items180/rifle.lr300.png", "lr-300", "0.34 0.72", "0.66 0.82");
            CreateItemButton(ref container, "build.givetools rifle.m39", "https://rustlabs.com/img/items180/rifle.m39.png", "m39 rifle", "0.68 0.72", "0.95 0.82");

            CreateItemButton(ref container, "build.givetools pistol.m92", "https://rustlabs.com/img/items180/pistol.m92.png", "m92 pistol", "0.05 0.59", "0.32 0.69");
            CreateItemButton(ref container, "build.givetools pistol.python", "https://rustlabs.com/img/items180/pistol.python.png", "python", "0.34 0.59", "0.66 0.69");
            CreateItemButton(ref container, "build.givetools pistol.revolver", "https://rustlabs.com/img/items180/pistol.revolver.png", "revolver", "0.68 0.59", "0.95 0.69");

            CreateItemButton(ref container, "build.givetools metal.facemask", "https://rustlabs.com/img/items180/metal.facemask.png", "metal facemask", "0.05 0.46", "0.32 0.56");
            CreateItemButton(ref container, "build.givetools metal.plate.torso", "https://rustlabs.com/img/items180/metal.plate.torso.png", "metal chestplate", "0.34 0.46", "0.66 0.56");
            CreateItemButton(ref container, "build.givetools roadsign.kilt", "https://rustlabs.com/img/items180/roadsign.kilt.png", "roadsign kilt", "0.68 0.46", "0.95 0.56");

            CreateItemButton(ref container, "build.givetools shoes.boots", "https://rustlabs.com/img/items180/shoes.boots.png", "boots", "0.05 0.33", "0.32 0.43");
            CreateItemButton(ref container, "build.givetools tactical.gloves", "https://rustlabs.com/img/items180/tactical.gloves.png", "tactical gloves", "0.34 0.33", "0.66 0.43");
            CreateItemButton(ref container, "build.givetools hoodie", "https://rustlabs.com/img/items180/hoodie.png", "hoodie", "0.68 0.33", "0.95 0.43");

            CreateItemButton(ref container, "build.givetools shotgun.pump", "https://rustlabs.com/img/items180/shotgun.pump.png", "pump shotgun", "0.05 0.20", "0.32 0.30");
            CreateItemButton(ref container, "build.givetools shotgun.spas12", "https://rustlabs.com/img/items180/shotgun.spas12.png", "spas-12", "0.34 0.20", "0.66 0.30");
            CreateItemButton(ref container, "build.givetools smg.thompson", "https://rustlabs.com/img/items180/smg.thompson.png", "thompson", "0.68 0.20", "0.95 0.30");
        }

        private void CreateExplosivesPage(ref CuiElementContainer container)
        {
            CreateItemButton(ref container, "build.givetools rocket.launcher", "https://rustlabs.com/img/items180/rocket.launcher.png", "rocket launcher", "0.05 0.72", "0.32 0.82");
            CreateItemButton(ref container, "build.giverockets ammo.rocket.basic", "https://rustlabs.com/img/items180/ammo.rocket.basic.png", "rocket (stack)", "0.34 0.72", "0.66 0.82");
            CreateItemButton(ref container, "build.giverockets ammo.rocket.hv", "https://rustlabs.com/img/items180/ammo.rocket.hv.png", "hv rocket (stack)", "0.68 0.72", "0.95 0.82");

            CreateItemButton(ref container, "build.givetools lmg.m249", "https://rustlabs.com/img/items180/lmg.m249.png", "m249", "0.05 0.59", "0.32 0.69");
            CreateItemButton(ref container, "build.giverockets ammo.rocket.fire", "https://rustlabs.com/img/items180/ammo.rocket.fire.png", "incendiary rocket (stack)", "0.34 0.59", "0.66 0.69");
            CreateItemButton(ref container, "build.giverockets ammo.rocket.smoke", "https://rustlabs.com/img/items180/ammo.rocket.smoke.png", "smoke rocket (stack)", "0.68 0.59", "0.95 0.69");

            CreateItemButton(ref container, "build.givetools explosive.timed", "https://rustlabs.com/img/items180/explosive.timed.png", "timed explosive", "0.05 0.46", "0.32 0.56");
            CreateItemButton(ref container, "build.givetools grenade.f1", "https://rustlabs.com/img/items180/grenade.f1.png", "f1 grenade", "0.34 0.46", "0.66 0.56");
            CreateItemButton(ref container, "build.givetools grenade.beancan", "https://rustlabs.com/img/items180/grenade.beancan.png", "beancan grenade", "0.68 0.46", "0.95 0.56");

            CreateItemButton(ref container, "build.givetools explosive.satchel", "https://rustlabs.com/img/items180/explosive.satchel.png", "satchel charge", "0.05 0.33", "0.32 0.43");
            CreateItemButton(ref container, "build.givetools surveycharge", "https://rustlabs.com/img/items180/surveycharge.png", "survey charge", "0.34 0.33", "0.66 0.43");
            CreateItemButton(ref container, "build.givetools grenade.molotov", "https://rustlabs.com/img/items180/grenade.molotov.png", "molotov", "0.68 0.33", "0.95 0.43");

            CreateItemButton(ref container, "build.givetools flamethrower", "https://rustlabs.com/img/items180/flamethrower.png", "flamethrower", "0.05 0.20", "0.32 0.30");
            CreateItemButton(ref container, "build.givetools multiplegrenadelauncher", "https://rustlabs.com/img/items180/multiplegrenadelauncher.png", "m79", "0.34 0.20", "0.66 0.30");
        }

        private void CreateAmmunitionPage(ref CuiElementContainer container)
        {
            CreateItemButton(ref container, "build.giveammo ammo.rifle", "https://rustlabs.com/img/items180/ammo.rifle.png", "5.56 rifle ammo (stack)", "0.05 0.72", "0.32 0.82");
            CreateItemButton(ref container, "build.giveammo ammo.rifle.hv", "https://rustlabs.com/img/items180/ammo.rifle.hv.png", "5.56 hv (stack)", "0.34 0.72", "0.66 0.82");
            CreateItemButton(ref container, "build.giveammo ammo.rifle.incendiary", "https://rustlabs.com/img/items180/ammo.rifle.incendiary.png", "5.56 incendiary (stack)", "0.68 0.72", "0.95 0.82");

            CreateItemButton(ref container, "build.giveammo ammo.pistol", "https://rustlabs.com/img/items180/ammo.pistol.png", "pistol ammo (stack)", "0.05 0.59", "0.32 0.69");
            CreateItemButton(ref container, "build.giveammo ammo.pistol.hv", "https://rustlabs.com/img/items180/ammo.pistol.hv.png", "pistol hv (stack)", "0.34 0.59", "0.66 0.69");
            CreateItemButton(ref container, "build.giveammo ammo.pistol.fire", "https://rustlabs.com/img/items180/ammo.pistol.fire.png", "pistol incendiary (stack)", "0.68 0.59", "0.95 0.69");

            CreateItemButton(ref container, "build.giveammo ammo.shotgun", "https://rustlabs.com/img/items180/ammo.shotgun.png", "12 gauge buckshot (stack)", "0.05 0.46", "0.32 0.56");
            CreateItemButton(ref container, "build.giveammo ammo.shotgun.slug", "https://rustlabs.com/img/items180/ammo.shotgun.slug.png", "12 gauge slug (stack)", "0.34 0.46", "0.66 0.56");
            CreateItemButton(ref container, "build.giveammo ammo.handmade.shell", "https://rustlabs.com/img/items180/ammo.handmade.shell.png", "handmade shell (stack)", "0.68 0.46", "0.95 0.56");

            CreateItemButton(ref container, "build.giveammo arrow.wooden", "https://rustlabs.com/img/items180/arrow.wooden.png", "wooden arrow (stack)", "0.05 0.33", "0.32 0.43");
            CreateItemButton(ref container, "build.giveammo arrow.hv", "https://rustlabs.com/img/items180/arrow.hv.png", "hv arrow (stack)", "0.34 0.33", "0.66 0.43");
            CreateItemButton(ref container, "build.giveammo arrow.fire", "https://rustlabs.com/img/items180/arrow.fire.png", "fire arrow (stack)", "0.68 0.33", "0.95 0.43");

            CreateItemButton(ref container, "build.giveammo ammo.nailgun.nails", "https://rustlabs.com/img/items180/ammo.nailgun.nails.png", "nails (stack)", "0.05 0.20", "0.32 0.30");
            CreateItemButton(ref container, "build.giveammo speargun.spear", "https://rustlabs.com/img/items180/speargun.spear.png", "speargun spear (stack)", "0.34 0.20", "0.66 0.30");
            CreateItemButton(ref container, "build.giveammo ammo.grenadelauncher.he", "https://rustlabs.com/img/items180/ammo.grenadelauncher.he.png", "40mm he (stack)", "0.68 0.20", "0.95 0.30");
        }

        private void CreateAttachmentsPage(ref CuiElementContainer container)
        {
            // Row 1
            CreateItemButton(ref container, "build.givetools weapon.mod.lasersight", "https://rustlabs.com/img/items180/weapon.mod.lasersight.png", "laser sight", "0.05 0.72", "0.32 0.82");
            CreateItemButton(ref container, "build.givetools weapon.mod.silencer", "https://rustlabs.com/img/items180/weapon.mod.silencer.png", "silencer", "0.34 0.72", "0.66 0.82");
            CreateItemButton(ref container, "build.givetools weapon.mod.8x.scope", "https://wiki.rustclash.com/img/items180/weapon.mod.8x.scope.png", "variable scope", "0.68 0.72", "0.95 0.82");

            // Row 2 - Fixed extended magazine
            CreateItemButton(ref container, "build.givetools weapon.mod.extendedmags", "https://wiki.rustclash.com/img/items180/weapon.mod.extendedmags.png", "extended magazine", "0.05 0.59", "0.32 0.69");
            CreateItemButton(ref container, "build.givetools weapon.mod.muzzlebrake", "https://rustlabs.com/img/items180/weapon.mod.muzzlebrake.png", "muzzle brake", "0.34 0.59", "0.66 0.69");
            CreateItemButton(ref container, "build.givetools weapon.mod.holosight", "https://rustlabs.com/img/items180/weapon.mod.holosight.png", "holosight", "0.68 0.59", "0.95 0.69");

            // Row 3
            CreateItemButton(ref container, "build.givetools weapon.mod.simplesight", "https://rustlabs.com/img/items180/weapon.mod.simplesight.png", "simple sight", "0.05 0.46", "0.32 0.56");
            CreateItemButton(ref container, "build.givetools weapon.mod.flashlight", "https://rustlabs.com/img/items180/weapon.mod.flashlight.png", "flashlight", "0.34 0.46", "0.66 0.56");
            CreateItemButton(ref container, "build.givetools weapon.mod.small.scope", "https://rustlabs.com/img/items180/weapon.mod.small.scope.png", "8x scope", "0.68 0.46", "0.95 0.56");

            // Row 4
            CreateItemButton(ref container, "build.givetools weapon.mod.muzzleboost", "https://rustlabs.com/img/items180/weapon.mod.muzzleboost.png", "muzzle boost", "0.05 0.33", "0.32 0.43");
            CreateItemButton(ref container, "build.givetools weapon.mod.burstmodule", "https://rustlabs.com/img/items180/weapon.mod.burstmodule.png", "burst module", "0.34 0.33", "0.66 0.43");
        }

        private void CloseBuildUI(BasePlayer player)
        {
            // Remove player from tracking sets
            playersWithUIOpen.Remove(player.userID);
            playerCurrentTab.Remove(player.userID);
            playerGearPage.Remove(player.userID);
            
            // Close UI properly using the UI name
            CuiHelper.DestroyUi(player, "BuildUI");
        }

        private void CreateItemButton(ref CuiElementContainer container, string command, string imageUrl, string tooltipText, string aMin, string aMax)
        {
            string buttonName = $"ItemBtn_{System.Guid.NewGuid().ToString("N")[..8]}";
            
            // Parse coordinates
            string[] coords = aMin.Split(' ');
            string[] coordsMax = aMax.Split(' ');
            float minX = float.Parse(coords[0]);
            float minY = float.Parse(coords[1]);
            float maxX = float.Parse(coordsMax[0]);
            float maxY = float.Parse(coordsMax[1]);
            
            // Calculate button center and size for square image
            float centerX = (minX + maxX) / 2f;
            float centerY = (minY + maxY) / 2f;
            float buttonHeight = maxY - minY;
            float imageSize = buttonHeight * 0.7f; // Make image 70% of button height
            float halfImageSize = imageSize / 2f;
            
            // Create background panel for the button
            UI.CreatePanel(ref container, "BuildUI", "0.2 0.2 0.2 0.8", aMin, aMax);
            
            // Create the image (square, centered)
            string imageMin = $"{centerX - halfImageSize} {centerY - halfImageSize + 0.02}"; // Slight upward offset
            string imageMax = $"{centerX + halfImageSize} {centerY + halfImageSize + 0.02}";
            UI.CreateImage(ref container, "BuildUI", imageUrl, buttonName + "_img", imageMin, imageMax);
            
            // Create invisible button over the entire button area
            UI.CreateButton(ref container, "BuildUI", "0 0 0 0", "", 1, aMin, aMax, command);
            
            // Create tooltip text at bottom of button
            string tooltipMin = $"{minX} {minY}";
            string tooltipMax = $"{maxX} {minY + 0.02}";
            UI.CreateLabel(ref container, "BuildUI", "0.8 0.8 0.8 1", tooltipText, 10, tooltipMin, tooltipMax);
        }

        [ConsoleCommand("build.switchtab")]
        void CmdSwitchTab(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            string tabName = arg.GetString(0);
            if (string.IsNullOrEmpty(tabName)) return;

            // Reset gear page when switching tabs
            if (tabName == "gear" && !playerGearPage.ContainsKey(player.userID))
                playerGearPage[player.userID] = 1;

            OpenBuildUI(player, tabName);
        }

        [ConsoleCommand("build.gearpage")]
        void CmdGearPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            int page = arg.GetInt(0, 1);
            if (page < 1 || page > 4) return;

            playerGearPage[player.userID] = page;
            OpenBuildUI(player, "gear");
        }

        [ConsoleCommand("build.giveitem")]
        void CmdGiveItem(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            string itemName = arg.GetString(0);
            if (string.IsNullOrEmpty(itemName)) return;

            // Give 1000 of the item directly
            Item item = ItemManager.CreateByName(itemName, 1000);
            if (item != null)
            {
                player.GiveItem(item);
            }

            // UI remains open after giving items
        }

        [ConsoleCommand("build.givetools")]
        void CmdGiveTools(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            string itemName = arg.GetString(0);
            if (string.IsNullOrEmpty(itemName)) return;

            // Give 1 of the tool/weapon/attachment
            Item item = ItemManager.CreateByName(itemName, 1);
            if (item != null)
            {
                player.GiveItem(item);
            }

            // UI remains open after giving tools
        }

        [ConsoleCommand("build.giveammo")]
        void CmdGiveAmmo(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            string itemName = arg.GetString(0);
            if (string.IsNullOrEmpty(itemName)) return;

            // Get the item definition to find max stack size
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemName);
            if (itemDef == null) return;

            // Give full stack of ammo (using item's max stack size)
            int stackSize = itemDef.stackable;
            Item item = ItemManager.CreateByName(itemName, stackSize);
            if (item != null)
            {
                player.GiveItem(item);
            }

            // UI remains open after giving ammo
        }

        [ConsoleCommand("build.giverockets")]
        void CmdGiveRockets(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            string itemName = arg.GetString(0);
            if (string.IsNullOrEmpty(itemName)) return;

            // Get the item definition to find max stack size
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemName);
            if (itemDef == null) return;

            // Give full stack of rockets (using item's max stack size)
            int stackSize = itemDef.stackable;
            Item item = ItemManager.CreateByName(itemName, stackSize);
            if (item != null)
            {
                player.GiveItem(item);
            }

            // UI remains open after giving rockets
        }

        [ConsoleCommand("build.giveskinnedtools")]
        void CmdGiveSkinnedTools(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            string itemName = arg.GetString(0);
            if (string.IsNullOrEmpty(itemName)) return;

            // Give hammer with skin ID 2601159326
            Item item = ItemManager.CreateByName(itemName, 1, 2601159326);
            if (item != null)
            {
                player.GiveItem(item);
            }

            // UI remains open after giving tools
        }

        [ConsoleCommand("build.giveretrotc")]
        void CmdGiveRetroTC(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            string itemName = arg.GetString(0);
            if (string.IsNullOrEmpty(itemName)) return;

            // Give retro tool cupboard using the ItemID
            Item item = ItemManager.CreateByItemID(1488606552, 1);
            if (item != null)
            {
                player.GiveItem(item);
            }

            // UI remains open after giving tools
        }

        [ConsoleCommand("build.giveoutfit")]
        void CmdGiveOutfit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PermUse)) return;

            // Define outfit items with their corresponding workshop skin IDs
            var outfitItems = new Dictionary<string, ulong>
            {
                { "hoodie", 1784482745 },
                { "riot.helmet", 1784490572 },
                { "santabeard", 2126889441 },
                { "pants", 1784474755 },
                { "shoes.boots", 2483680748 },
                { "burlap.gloves", 1552705918 },
                { "largebackpack", 3570044229 }
            };
            
            // Give complete outfit with skins
            foreach (var kvp in outfitItems)
            {
                Item item = ItemManager.CreateByName(kvp.Key, 1, kvp.Value);
                if (item != null)
                {
                    player.GiveItem(item);
                }
            }

            // UI remains open after giving outfit
        }

        [ConsoleCommand("build.closeui")]
        void CmdCloseUI(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            CloseBuildUI(player);
            player.ChatMessage("Build UI closed.");
        }

        #region UI Helper Class
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string parent, string panelName, string color, string aMin, string aMax, bool useCursor)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                return NewElement;
            }
            
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            
            static public void CreateImage(ref CuiElementContainer container, string panel, string url, string name, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = url
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax
                        }
                    }
                });
            }
            
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }
            
            static public void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
        }
        #endregion
    }
}