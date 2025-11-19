using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Patrol Helicopter Rotor Health", "handcaster", "1.0.0")]
    [Description("Shows main and tail rotor health when targeting patrol helicopter")]
    public class PHRH : RustPlugin
    {
        private const string UI_NAME = "HelicopterRotorUI";
        private Dictionary<ulong, float> lastUpdateTime = new Dictionary<ulong, float>();
        private Dictionary<ulong, float> lastTargetTime = new Dictionary<ulong, float>();
        private const float UPDATE_INTERVAL = 0.1f; // Update every 100ms
        private const float HIDE_DELAY = 1.0f; // 1 second delay before hiding UI
        
        #region Hooks
        
        void OnServerInitialized()
        {
            timer.Every(UPDATE_INTERVAL, CheckPlayersTargeting);
        }
        
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }
        
        #endregion
        
        #region Core Logic
        
        void CheckPlayersTargeting()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null || !player.IsConnected) continue;
                
                // Check if player is targeting a patrol helicopter
                PatrolHelicopter heli = GetTargetedHelicopter(player);
                
                if (heli != null && IsPatrolHelicopterActive())
                {
                    ShowRotorHealthUI(player, heli);
                    lastUpdateTime[player.userID] = Time.time;
                    lastTargetTime[player.userID] = Time.time;
                }
                else
                {
                    // Check if we should hide the UI after delay
                    if (lastTargetTime.ContainsKey(player.userID))
                    {
                        float timeSinceLastTarget = Time.time - lastTargetTime[player.userID];
                        if (timeSinceLastTarget >= HIDE_DELAY)
                        {
                            DestroyUI(player);
                            lastUpdateTime.Remove(player.userID);
                            lastTargetTime.Remove(player.userID);
                        }
                    }
                }
            }
        }
        
        PatrolHelicopter GetTargetedHelicopter(BasePlayer player)
        {
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 1000f))
            {
                PatrolHelicopter heli = hit.GetEntity()?.GetComponent<PatrolHelicopter>();
                if (heli != null)
                {
                    return heli;
                }
            }
            return null;
        }
        
        bool IsPatrolHelicopterActive()
        {
            // Check if any patrol helicopter exists on the map
            foreach (PatrolHelicopter heli in UnityEngine.Object.FindObjectsOfType<PatrolHelicopter>())
            {
                if (heli != null && !heli.IsDead())
                {
                    return true;
                }
            }
            return false;
        }
        
        #endregion
        
        #region UI Creation
        
        void ShowRotorHealthUI(BasePlayer player, PatrolHelicopter heli)
        {
            // Get rotor health values
            float mainRotorHealth = GetMainRotorHealth(heli);
            float tailRotorHealth = GetTailRotorHealth(heli);
            float mainRotorMaxHealth = GetMainRotorMaxHealth(heli);
            float tailRotorMaxHealth = GetTailRotorMaxHealth(heli);
            
            DestroyUI(player); // Remove existing UI first
            
            CuiElementContainer container = new CuiElementContainer();
            
            // Main container - positioned above hotbar with more transparency and smaller size
            container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.6" },
                RectTransform = { AnchorMin = "0.42 0.16", AnchorMax = "0.58 0.24" },
                CursorEnabled = false
            }, "Overlay", UI_NAME);
            
            // Main Rotor Label
            container.Add(new CuiLabel
            {
                Text = { Text = "Main Rotor", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" },
                RectTransform = { AnchorMin = "0.05 0.65", AnchorMax = "0.6 0.95" }
            }, UI_NAME);
            
            // Main Rotor HP Value
            container.Add(new CuiLabel
            {
                Text = { Text = $"{mainRotorHealth:F0}/{mainRotorMaxHealth:F0}", FontSize = 12, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.6 0.65", AnchorMax = "0.95 0.95" }
            }, UI_NAME);
            
            // Main Rotor Health Bar Background
            container.Add(new CuiPanel
            {
                Image = { Color = "0.2 0.2 0.2 0.8" },
                RectTransform = { AnchorMin = "0.05 0.52", AnchorMax = "0.95 0.6" }
            }, UI_NAME, UI_NAME + "_MainRotorBG");
            
            // Main Rotor Health Bar - Green/Yellow based on health
            float mainRotorPercent = (mainRotorHealth / mainRotorMaxHealth) * 100f;
            container.Add(new CuiPanel
            {
                Image = { Color = GetHealthBarColor(mainRotorPercent) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"{mainRotorPercent / 100f} 1" }
            }, UI_NAME + "_MainRotorBG");
            
            // Tail Rotor Label
            container.Add(new CuiLabel
            {
                Text = { Text = "Tail Rotor", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" },
                RectTransform = { AnchorMin = "0.05 0.25", AnchorMax = "0.6 0.45" }
            }, UI_NAME);
            
            // Tail Rotor HP Value
            container.Add(new CuiLabel
            {
                Text = { Text = $"{tailRotorHealth:F0}/{tailRotorMaxHealth:F0}", FontSize = 12, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.6 0.25", AnchorMax = "0.95 0.45" }
            }, UI_NAME);
            
            // Tail Rotor Health Bar Background
            container.Add(new CuiPanel
            {
                Image = { Color = "0.2 0.2 0.2 0.8" },
                RectTransform = { AnchorMin = "0.05 0.12", AnchorMax = "0.95 0.2" }
            }, UI_NAME, UI_NAME + "_TailRotorBG");
            
            // Tail Rotor Health Bar - Green/Yellow based on health
            float tailRotorPercent = (tailRotorHealth / tailRotorMaxHealth) * 100f;
            container.Add(new CuiPanel
            {
                Image = { Color = GetHealthBarColor(tailRotorPercent) },
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"{tailRotorPercent / 100f} 1" }
            }, UI_NAME + "_TailRotorBG");
            
            CuiHelper.AddUi(player, container);
        }
        
        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_NAME);
        }
        
        #endregion
        
        #region Health Calculation
        
        float GetMainRotorHealth(PatrolHelicopter heli)
        {
            // Try different possible property names for main rotor health
            if (heli.weakspots != null && heli.weakspots.Length > 0)
            {
                // Main rotor is typically the first weakspot
                return heli.weakspots[0].health;
            }
            return heli.health; // Fallback to overall health
        }
        
        float GetTailRotorHealth(PatrolHelicopter heli)
        {
            // Try different possible property names for tail rotor health
            if (heli.weakspots != null && heli.weakspots.Length > 1)
            {
                // Tail rotor is typically the second weakspot
                return heli.weakspots[1].health;
            }
            return heli.health; // Fallback to overall health
        }
        
        float GetMainRotorMaxHealth(PatrolHelicopter heli)
        {
            if (heli.weakspots != null && heli.weakspots.Length > 0)
            {
                return heli.weakspots[0].maxHealth;
            }
            return heli.MaxHealth(); // Fallback to overall max health
        }
        
        float GetTailRotorMaxHealth(PatrolHelicopter heli)
        {
            if (heli.weakspots != null && heli.weakspots.Length > 1)
            {
                return heli.weakspots[1].maxHealth;
            }
            return heli.MaxHealth(); // Fallback to overall max health
        }
        
        #endregion
        
        #region UI Helpers
        
        string GetHealthBarColor(float percent)
        {
            if (percent > 50) return "0.4 0.8 0.2 0.9"; // Bright green matching your screenshot
            return "0.8 0.8 0.2 0.9"; // Yellow matching your screenshot
        }
        
        #endregion
    }
}