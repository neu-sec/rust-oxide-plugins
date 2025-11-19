# Rust Oxide Plugins

A collection of custom Oxide plugins for Rust dedicated servers, designed to enhance gameplay and server management.

## 📋 Table of Contents

- Overview
- Plugins
- Installation
- Configuration
- Permissions
- Commands
- Requirements
- Support
- License

## 🎮 Overview

This repository contains custom plugins for Rust servers running the Oxide/uMod framework. These plugins add new features, custom weapons, UI improvements, and gameplay enhancements.

## 🔌 Plugins

### Build (v1.1.0)

A comprehensive building materials and gear management UI that allows players to easily access resources, weapons, armor, and attachments.

**Features:**
- Tabbed UI interface with Building, Decorations, and Gear sections
- Quick access to building materials (Wood, Stone, Metal, HQM)
- Complete weapon and armor sets
- Ammunition and attachments management
- Custom outfit support with skins
- ESC key closes UI naturally

**Commands:**
- `/build` or `/b` - Opens the build UI

**Permissions:**
- `build.use` - Access to the build UI

---

### Hand Cannon (v1.0.0)

A custom weapon that transforms the Python revolver into a rocket-firing hand cannon.

**Features:**
- Fires high-velocity rockets instead of bullets
- 6-round capacity
- Configurable projectile speed
- Custom weapon skins support
- Uses HV rockets as ammunition

**Commands:**
- `/givehc` - Gives a Hand Cannon to the player
- `/hcskin <skinid>` - Applies a skin to your Hand Cannon

**Permissions:**
- `handcannon.give` - Ability to spawn Hand Cannons

---

### Mini Death (v1.0.0)

Converts the minigun into a devastating rocket launcher with massive ammo capacity.

**Features:**
- Fires high-velocity rockets with 8x damage multiplier
- 300-round magazine capacity
- Adjustable projectile speed
- Custom weapon naming and skins
- Automatic reload system

**Commands:**
- `/givemd` - Gives a Mini Death weapon
- `/mdskin <skinid>` - Applies a skin to your Mini Death

**Permissions:**
- `minideath.give` - Ability to spawn Mini Death weapons

---

### PHRH - Patrol Helicopter Rotor Health (v1.0.0)

Displays real-time health information for patrol helicopter rotors when targeting them.

**Features:**
- Shows main and tail rotor health bars
- Auto-updates every 100ms while targeting
- Color-coded health indicators (green/yellow)
- Automatically hides after 1 second delay
- Positioned above hotbar for easy viewing

**No commands or permissions required** - works automatically for all players.

## 📥 Installation

1. Download the desired plugin `.cs` files from this repository
2. Place the files in your Rust server's `oxide/plugins` directory
3. The plugins will automatically compile and load
4. Configure settings in `oxide/config` if needed

## ⚙️ Configuration

### Hand Cannon Configuration

````json
{
  "HandCannonSkinId": "0",
  "HandCannonDisplayName": "Hand Cannon",
  "MaxAmmoCapacity": 6,
  "AmmoType": "ammo.rocket.hv",
  "ProjectileSpeed": 200.0
}
````

### Mini Death Configuration

````json
{
  "MiniDeathSkinId": "0",
  "MiniDeathDisplayName": "Mini Death",
  "MaxAmmoCapacity": 300,
  "AmmoType": "ammo.rocket.hv",
  "ProjectileSpeed": 300.0,
  "DamageMultiplier": 8.0
}
````

## 🔐 Permissions

Grant permissions using: `oxide.grant user <username> <permission>` or `oxide.grant group <groupname> <permission>`

| Plugin | Permission | Description |
|--------|-----------|-------------|
| Build | `build.use` | Access to build UI |
| Hand Cannon | `handcannon.give` | Spawn Hand Cannons |
| Mini Death | `minideath.give` | Spawn Mini Death weapons |

## 💻 Commands

| Command | Plugin | Description |
|---------|--------|-------------|
| `/build` or `/b` | Build | Opens build UI |
| `/givehc` | Hand Cannon | Gives Hand Cannon |
| `/hcskin <id>` | Hand Cannon | Apply skin to Hand Cannon |
| `/givemd` | Mini Death | Gives Mini Death weapon |
| `/mdskin <id>` | Mini Death | Apply skin to Mini Death |

## 📦 Requirements

- Rust Dedicated Server
- Oxide/uMod framework (latest version recommended)
- Server restart or plugin reload after installation

## 🤝 Support

For issues, questions, or contributions:

1. Check existing issues in the repository
2. Create a new issue with detailed information
3. Include server logs if reporting bugs
4. Specify Rust and Oxide versions

## ⚠️ Disclaimer

These plugins are provided as-is for use on private Rust servers. Server administrators are responsible for:

- Ensuring plugins comply with their server's rules
- Monitoring plugin usage and balance
- Regular backups before installing new plugins
- Testing plugins in a development environment first

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Note:** Always backup your server before installing new plugins. Test thoroughly in a development environment before deploying to production servers.