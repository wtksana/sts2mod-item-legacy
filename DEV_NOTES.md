# Item Legacy 开发记录

## 目标

开发一个“杀戮尖塔2”休息点相关 mod，工程目录为 `item-legacy`。

## 当前功能设计结论

### 目标功能

在休息点新增一个 `遗产` 选项。

玩家点击后：

- 从上一局游戏结束时保留的卡牌中选 1 张获得
- 从上一局游戏结束时保留的全部药水中逐个领取，可按原版奖励页逻辑跳过
- 从上一局游戏结束时保留的遗物中选 1 个获得

### 当前推荐实现路线

优先复用原版奖励体系，而不是自建新 UI：

1. 通过 Harmony 在休息点追加一个新的 `LegacyRestSiteOption`
2. 读取 `SaveManager` 保存的最新 `RunHistory`
3. 从对应 `RunHistoryPlayer` 中恢复上一局结束时的卡牌、药水、遗物
4. 依次打开原版奖励页完成三类选择

## 当前实现说明

### 休息点入口

- 当前版本沿用 `rest-site-market` 已验证过的接入方式：
  - `RestSiteOption.Generate` 追加自定义选项
  - `NRestSiteButton.Reload` 设置自定义标题
  - `RestSiteOption.get_Icon` 提供图标兜底
  - `NRestSiteButton.RefreshTextState` 显示自定义描述
  - 通过输入中继解决首击触发不稳定问题

### 上一局数据来源

- 当前版本直接读取 `SaveManager.Instance.GetAllRunHistoryNames()`
- 取时间戳最大的 `*.run` 作为“上一局已结束的历史记录”
- 优先匹配当前本地玩家 ID；若历史中不存在本地玩家，则回退到同角色玩家，再回退到首个玩家

### 奖励发放方式

- 卡牌：
  - 当前版本使用自定义 `LegacyCardReward`
  - 展示层复用原版奖励页
  - 领取时才把历史卡牌注册进当前 `RunState`，避免未选中的卡污染当前局状态
- 药水：
  - 直接复用 `PotionReward`
  - 当前阶段以普通奖励列表展示，不使用 `LinkedRewardSet`，因此不会限制为只能拿 1 瓶
- 遗物：
  - 直接复用 `RelicReward`

## 已验证

- 2026-05-18：游戏 2026-05-17 版本将原版奖励同步流程重构为 `RewardsSetSynchronizer` 驱动，`RewardsSet.Offer()` 通过 `BeginRewardsSet` 拿到 `Task` 后由同步器在 `AllRewardsSuccessfullySelected` 时完成；`NRewardButton.GetReward` 改走 `RewardsSetSynchronizer.SelectLocalReward`；`Reward.Populate` 由 `Task` 改为 `void`。当前版本：`LegacyCardReward.Populate` 改为 `void` 空实现；卡牌/遗物的链式奖励组改用 `LegacyLinkedRewardSet : LinkedRewardSet`，在子奖励成功领取后通过反射把容器自身的 `Reward.SuccessfullySelected` 设为 true，从而让外层 `RewardsSet` 可以被同步器自然完成。
- 2026-05-18：新版 `NLinkedRewardSet.Reload` 把 1 参的 `RewardClaimed` 信号用无参 `Callable.From(GetReward)` 接，触发时 Godot 抛 `ArgCountMismatch` 把 callable 吞掉，导致链式奖励组的 `GetReward` → `_rewardsScreen.RewardCollectedFrom` 永不触发，UI 容器无法关闭、新一段 `Offer()` 直接叠新窗口。表现为「全部继承完后又冒出一个不能跳过、点物品也不会获得的窗口」。修复：复活 `LegacyLinkedRewardSetReloadPatch`，Prefix 重写 Reload，把 callable 改成 1 参 lambda 重新接信号；`OnLinkedRewardClaimed` 内部仍调 `LinkedRewardSet.OnSkipped()` 以写入未选兄弟奖励的 `wasPicked=false` 历史记录。
- 2026-05-18：`NRewardButton.GetReward` 在新版会调用 `RewardsSetSynchronizer.SelectLocalReward(reward)`，由于子奖励不在顶层 `RewardsSet.Rewards` 中，本地会发出 `rewardIndex=-1` 的 `RewardSelectedMessage`。远端因为 mod 跳过了 `Offer()`，对应玩家槽位的 `nextId` 一直为 0，消息会被塞进 buffer 但永远不被消费，实际无副作用，仅有少量挂起消息累积。
- 2026-04-19：已确认 `RunHistory` 中直接保存了上一局结束时的 `deck`、`potions`、`relics`，不需要额外从“历史记录 UI”反向抓取显示节点。
- 2026-04-19：已确认 `SaveManager.Instance.GetAllRunHistoryNames()` + `LoadRunHistory(...)` 可以直接读取历史局存档，文件名使用 `StartTime.run`。
- 2026-04-19：已确认原版 `RewardsSet.WithCustomRewards(...).Offer()` 可直接复用原版奖励页。
- 2026-04-19：已确认 `RelicReward`、`PotionReward` 支持传入指定模型，适合直接承载上一局遗物/药水选择。
- 2026-04-19：已确认直接复用 `SpecialCardReward` 会让未选中的历史卡牌提前进入当前 `RunState`；当前版本已改为自定义 `LegacyCardReward`，只在真正领取时注册卡牌。
- 2026-04-25：main 分支卡牌遗产保持只按上一局历史中的卡牌 `Id` 继承基础版卡牌，不继承升级等级和附魔状态；同名卡候选仍按 `Id` 去重。
- 2026-04-26：`ItemLegacy.cfg` 支持 `[Cards] Enabled` 控制卡牌遗产是否启用，`InheritUpgrades` 控制是否继承升级，`InheritEnchantments` 控制是否继承附魔；默认启用卡牌，但不继承升级和附魔，保持 main 行为。只开启升级时不会保留历史卡牌 `Props`，避免把非升级状态一起带入；开启附魔时会保留附魔和相关保存属性。历史卡牌的 `FloorAddedToDeck` 不继承，当前局获得楼层仍交给原版 `CardPileCmd.Add` 写入。
- 2026-04-19：已验证 `dotnet build C:\Dev\sts2mod\item-legacy\ItemLegacy.csproj` 可成功构建，输出 DLL 为 `C:\Dev\sts2mod\item-legacy\.godot\mono\temp\bin\Debug\ItemLegacy.dll`。
- 2026-04-19：已改为使用 `LinkedRewardSet` 承载三类遗产，当前实现为“卡牌三选一 / 药水三选一 / 遗物三选一”，不再错误地把整组奖励全部领走。
- 2026-04-19：药水遗产阶段已改为复用原版 `PotionReward`，当前药水栏已满时不会自动跳过，但该阶段允许玩家按原版奖励页逻辑直接跳过，不额外弹出替换药水选择。
- 2026-04-26：药水遗产阶段改为普通奖励列表，上一局可继承药水都会展示并可逐个领取，不再用 `LinkedRewardSet` 限制为只能拿 1 瓶。
- 2026-04-19：`LegacyRunClaimTracker` 直接读取原版 `RunState.MapPointHistory -> PlayerMapPointHistoryEntry.RestSiteChoices` 判断本局是否已领取遗产，不再写独立侧车状态文件；这样休息处内读档回滚时，遗产状态会与原版运行存档保持一致。
- 2026-04-19：卡牌、药水、遗物三类遗产列表在进入奖励页前会先去重；当前策略为卡牌按 `SerializableCard` 完整相等去重，药水/遗物按模型 `Id` 去重。
- 2026-04-19：三类遗产阶段全部允许跳过，不再对任一阶段禁用原版奖励页的 `Skip`。
- 2026-04-19：实测发现原版 `NLinkedRewardSet` 在当前环境下没有按预期在领取后立即收起整组；当前版本已用 Harmony 接管其 `Reload` 中的子按钮回调，确保“每类最多拿一个”在选中任意一项后立即关闭本组。
- 2026-04-19：当某类遗产去重后只剩 1 个选项时，当前版本不再使用 `LinkedRewardSet`，直接用普通奖励页展示该单项。
- 2026-04-19：已确认休息处选项是否会消耗本次休息处，取决于 `RestSiteOption.OnSelect()` 是否返回 `true`；当前版本已将“完成遗产流程”与“实际拿到物品”解耦，只要进入遗产流程并正常结束，即视为本次休息处已消费，并由原版 `RestSiteChoices` 记录“本局已领取遗产”状态。
- 2026-04-25：main 分支遗物遗产保持只允许继承 `Common`、`Uncommon`、`Rare`、`Shop` 四类遗物，排除初始、远古、事件和无稀有度遗物。
- 2026-04-26：遗物遗产配置支持 `[Relics] Enabled` 控制是否启用，`InheritableRarities` 用英文逗号分隔枚举名精确配置可继承种类；默认 `Common, Uncommon, Rare, Shop`，也支持 `All`。
- 2026-04-26：新增 `[Potions] Enabled` 控制药水遗产是否启用，默认 `true`。
- 2026-04-26：新增 `[Gold] Enabled` 控制金币遗产是否启用；金币数从上一局 `RunHistory.MapPointHistory` 中对应玩家最后一次 `CurrentGold` 读取，默认 `false`。
- 2026-04-25：多人局遗产流程已改为“仅本地玩家客户端生成自己的遗产候选，并继续复用原版 `RewardSynchronizer` 同步领取/跳过结果”；非本地玩家对应的 `LegacyRestSiteOption` 不再读取本机历史记录，避免把别人的遗产候选错误地按本机历史重算。

## 限制/坑点

- 2026-04-19：`CardReward` 的原版选卡界面不适合直接展示整副上一局牌组，因为选项过多时会严重超出横向布局；当前版本改为把卡牌遗产也放到原版奖励页中滚动选择。
- 2026-04-19：当前休息点 `遗产` 按钮暂时复用 `smith` 图标兜底，后续如果补正式资源可移除这层补丁。
- 2026-04-19：当前构建依赖读取 `C:\Users\ttat\AppData\Roaming\NuGet\NuGet.Config`；在本工作区内直接执行 `dotnet build` 可能因权限不足失败，需要提权运行。
- 2026-04-19：为了与原版休息/锻造语义一致，遗产流程即使三段都 `Skip`，也必须返回成功；否则 `RestSiteSynchronizer` 不会清空后续休息处选项，也不会把本局遗产写入原版 `RestSiteChoices`。
- 2026-04-19：如果把“遗产已领取”额外写到独立文件，会出现休息处内 SL 后“奖励回档但遗产锁定未回档”的状态撕裂；当前版本已移除这条实现路线。
- 2026-04-25：原版联机会在入房阶段校验 `ModManager.GetGameplayRelevantModNameList()`；`item-legacy` 当前 `affects_gameplay=true`，因此“有人装 mod、有人没装 mod”正常情况下会直接触发 `ModMismatch`，不能作为兼容目标。
- 2026-04-26：配置文件使用类 BepInEx 的 `ItemLegacy.cfg`，带注释、分组和默认值说明；不使用 `*.json`，因为游戏原版 `ModManager` 会递归读取 mod 目录下所有 `*.json` 并尝试当作 manifest 解析。

## 命令

- 构建：`dotnet build C:\Dev\sts2mod\item-legacy\ItemLegacy.csproj`
- 部署：`pwsh -NoProfile -ExecutionPolicy Bypass -File C:\Dev\sts2mod\item-legacy\Deploy.ps1`
- 配置文件：`C:\Dev\sts2mod\GameInstall\mods\ItemLegacy\ItemLegacy.cfg`
- 更新日志：`C:\Dev\sts2mod\GameInstall\mods\ItemLegacy\更新日志.md`
