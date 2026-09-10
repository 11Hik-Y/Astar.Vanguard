# Astar Vanguard / 星锋战术支援

Astar Vanguard 是面向 **SPT 4.0.13 / EFT 40087 / Fika Host & Solo Host** 的本地 AI 战术支援 Mod。

本项目由 Astar 独立维护，最初基于 MiyakoCarryService 的 CC BY-NC-SA 4.0 源码进行改编；当前已形成独立的产品名称、版本线、UI、干员系统与后续开发路线。原项目归因、固定基线与修改说明见 `THIRD_PARTY_NOTICES.md`。

## 当前能力

- 服务端商人：**星锋指挥部**
- 兼容 Trader ID：`6952ced4bcc1dd1e3c80dfcb`
- 本地固定干员：37 名
- 干员身份：由 Server 生成并持久化到角色 Profile
- 干员能力：由 Fika Host / Solo Host 在 EFT 原生 Bot Settings 上执行
- 指挥中心 UI：通过本地 `Astar.UI.dll` 提供基础 UI Runtime
- 不包含赞助名单、赞助平台刷新、自动更新或联网版本检查

## 干员能力目录

每名干员拥有独立的：

`Aim / Vision / Hearing / Reaction / Aggression / DamageCoeff / EnemyMemory / Cover`

能力目录位于 `Server/Assets/database/operators/vanguard.json`。Client 构建时嵌入同一份 JSON，避免 Server 与 Host 使用两份手工维护的数据。

## 权威边界

1. **Server**：选择干员身份、写入并持久化角色 Profile。
2. **Fika Host / Solo Host**：创建 AI 时读取已固化代号，并应用原生 EFT AI 参数。
3. **Remote Client**：不负责判定这些 AI 能力；当前没有为干员能力新增 Packet / RPC。

## 源码构建

`Ref/` 是本地编译依赖缓存，不属于源码仓库，也不会上传到 GitHub。它包含 EFT、SPT、BepInEx、HarmonyX、Fika、BigBrain 等本机构建所需程序集。

先准备依赖：

```powershell
.\Scripts\copy_ref_dlls.ps1
```

脚本默认从本机 SPT 4.0.13 / EFT 40087 环境复制第三方程序集，并尝试从同级本地 `Astar.UI` 仓库的 `artifacts/bin/Release` 或 `Debug` 中获取 `Astar.UI.dll`。也可以显式指定：

```powershell
.\Scripts\copy_ref_dlls.ps1 -AstarUiDll "C:\path\to\Astar.UI.dll"
```

当前 Astar.UI 仍是内部开发依赖，尚未单独公开源码。

然后构建：

```powershell
dotnet build .\Astar.Vanguard.slnx -c Debug
dotnet build .\Astar.Vanguard.slnx -c Release
```

## 许可证

Astar Vanguard 作为包含 MiyakoCarryService 改编内容的组合衍生作品，按 **CC BY-NC-SA 4.0** 发布。原始部分继续归其各自作者所有；Astar 新增的原创代码、重构、文档、产品名称与新系统归 Astar 所有，并随本组合衍生作品按上述许可证发布。

本项目与 MiyakoCarryService、Plain Craft Launcher、SPT、Fika 及其他第三方项目不存在官方隶属、赞助或背书关系。详细归因与第三方说明见 `THIRD_PARTY_NOTICES.md`。
