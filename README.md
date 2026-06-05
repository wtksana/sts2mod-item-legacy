# Item Legacy

为《Slay the Spire 2》新增休息处选项“遗产”。进入休息处后，玩家可以从上一局历史记录中继承部分资源，作为本局的一次性奖励。

## 功能

- 在休息处新增“遗产”选项。
- 每局游戏只能领取一次遗产。
- 选择遗产后会像原版休息/锻造一样消费本次休息处选项。
- 支持从上一局结束状态继承卡牌、药水、遗物和金币。
- 卡牌、药水、遗物候选会先按类型去重。
- 奖励界面复用原版奖励页，支持原版跳过逻辑。
- 多人局中仅本地玩家客户端根据自己的历史记录生成自己的遗产，领取和跳过结果继续复用原版奖励同步。

## 默认规则

- 卡牌：从上一局卡牌中选 1 张获得，默认只继承同名基础版，不继承升级和附魔。
- 药水：上一局可继承药水会全部展示，可逐个领取，也可以跳过。
- 遗物：从上一局遗物中选 1 个获得，默认只允许普通、罕见、稀有、商店类遗物。
- 金币：默认关闭；开启后继承上一局结束时保留的金币。

## 配置

配置文件为 `ItemLegacy.cfg`，放在游戏 mod 目录：

```text
Slay the Spire 2/mods/ItemLegacy/ItemLegacy.cfg
```

修改配置后需要重启游戏。

默认配置：

```ini
[Cards]
Enabled = true
InheritUpgrades = false
InheritEnchantments = false

[Potions]
Enabled = true

[Relics]
Enabled = true
InheritableRarities = Common, Uncommon, Rare, Shop

[Gold]
Enabled = false
```

### 配置说明

- `Cards.Enabled`：是否启用卡牌遗产。
- `Cards.InheritUpgrades`：是否继承上一局卡牌升级等级。
- `Cards.InheritEnchantments`：是否继承上一局卡牌附魔。
- `Potions.Enabled`：是否启用药水遗产。
- `Relics.Enabled`：是否启用遗物遗产。
- `Relics.InheritableRarities`：允许继承的遗物种类，多个值用英文逗号分隔。
- `Gold.Enabled`：是否启用金币遗产。

可配置的遗物种类：

- `None`：无稀有度
- `Starter`：初始
- `Common`：普通
- `Uncommon`：罕见
- `Rare`：稀有
- `Shop`：商店
- `Event`：事件
- `Ancient`：远古
- `All`：允许所有种类

## 安装

将以下文件放入游戏安装目录的 `mods/ItemLegacy` 文件夹：

- `ItemLegacy.json`
- `ItemLegacy.dll`
- `ItemLegacy.cfg`
- `更新日志.txt`

示例结构：

```text
Slay the Spire 2/
`-- mods/
    `-- ItemLegacy/
        |-- ItemLegacy.json
        |-- ItemLegacy.dll
        |-- ItemLegacy.cfg
        `-- 更新日志.txt
```

## 构建

本项目依赖游戏安装目录中的 `sts2.dll` 和 `0Harmony.dll`。当前工程默认从 `C:\Programs\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64` 引用这些程序集。

```powershell
dotnet build .\ItemLegacy.csproj
```

如需使用其他游戏安装目录，可通过 MSBuild 属性覆盖：

```powershell
dotnet build .\ItemLegacy.csproj -p:GameInstallDir="D:\SteamLibrary\steamapps\common\Slay the Spire 2"
```

构建产物默认输出到：

```text
C:\Dev\sts2mod-item-legacy\.godot\mono\temp\bin\Debug\ItemLegacy.dll
```

## 部署到本地测试目录

仓库提供了本地部署脚本：

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Deploy.ps1
```

默认复制到：

```text
C:\Programs\Steam\steamapps\common\Slay the Spire 2\mods\ItemLegacy
```

脚本会复制：

- `ItemLegacy.dll`
- `ItemLegacy.json`
- `ItemLegacy.cfg`
- `更新日志.txt`

## 多人游戏

`ItemLegacy` 是 gameplay mod，manifest 中 `affects_gameplay` 为 `true`。原版联机会校验影响玩法的 mod 列表，因此装了本 mod 的玩家通常需要与同样安装本 mod 的玩家一起联机。

多人局中，每个本地玩家客户端只读取自己的历史记录来生成自己的遗产候选，不会用本机历史记录替其他玩家生成遗产。

## 更新日志

见 [更新日志.txt](更新日志.txt)。
