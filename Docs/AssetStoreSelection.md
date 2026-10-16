# Asset Store 美术接入

查询日期：2026-10-13。目标：Unity 6000.3.26f1 / URP，低多边形经营与冒险。

以下六包已由用户领取，已通过 Unity Package Manager 下载并导入本机项目。导入仅保留美术、材质、预制体与动画，不导入示例场景、示例控制脚本及 Packages/manifest.json。

| 类别 | 候选 | 项目用途 | 链接 |
| --- | --- | --- | --- |
| 角色 | Creative Characters FREE - Animated Pack | 玩家与村民模型、基础动画；页面列出 Unity 6 与 URP 支持 | https://assetstore.unity.com/packages/3d/characters/humanoids/creative-characters-free-animated-pack-304841 |
| 环境与植被 | Low Poly Environment - Nature Free | 树木、岩石、草地与森林；页面列出 URP 支持 | https://assetstore.unity.com/packages/3d/environments/low-poly-environment-nature-free-lowpoly-medieval-fantasy-series-187052 |
| 建筑 | Simple Modular Castle Assets | 城墙、门、塔、锯木厂和矿场；页面列出 URP 支持 | https://assetstore.unity.com/packages/3d/props/simple-modular-castle-assets-309297 |
| 市场与道具 | Medieval Markets Pack Demo | 市集棚、食物、陶器与工匠道具；免费版规模有限，页面列出 URP 支持 | https://assetstore.unity.com/packages/3d/environments/medieval-markets-pack-demo-336926 |
| 动画 | Human Basic Motions FREE | 人形角色移动与待机动画，需匹配 Humanoid 骨架 | https://assetstore.unity.com/packages/3d/animations/human-basic-motions-free-154271 |
| 生物与敌人 | RPG Monster Couple PBR Polyart | 怪物模型候选，保留项目自己的伙伴名称与能力 | https://marketplace.unity.com/packages/3d/characters/creatures/rpg-monster-couple-pbr-polyart-355988 |

## 当前接入

- 玩家与村长：Creative Characters 模块化角色，重新组合服装、发型与帽子；Human Basic Motions 提供 Humanoid 待机、行走混合动画，禁用根运动。sss
- 森林、岩石、矿石、倒木、草丛、花朵：Polytope 模型；导入发行者随包附带的 `PT_Nature_Free_URP_17.unitypackage`，保留植被专用材质。
- 集市：Medieval Markets 完整鱼市棚与摊位道具。
- 工坊与图书馆：Lumber Mill 与 Stone Tower；原有建造条件与存档兼容。
- 伙伴和守卫：两种 Polyart 怪物作临时外观。三个伙伴种类仍沿用原有 ID、名称和能力，暂未拥有三套各自独立的原创造型。

本地美术配置：`Assets/StarfieldEra/Resources/StoreVisuals.asset`。角色组合预制体存放在 `Assets/StarfieldEra/LocalStoreArt`。这些文件和第三方原始资源均由 `.gitignore` 排除，公开仓库只保存接入代码和文档。缺少配置时继续显示原有程序化占位外观。

## 换机器恢复

1. 使用领取资源的 Unity 账号，在 Package Manager → My Assets 下载并导入以上六包（不需要示例场景或脚本）。
2. 从自然包附带的 URP 文件夹导入 URP 17 支持包。城堡预制体使用发行者的 Toon_URP 材质，怪物包默认已有 URP 材质。
3. 点击 `StarfieldEra → Connect claimed store art` 自动重建本地角色组合和配置，再打开 FarmPrototype 场景运行。

本次只做编译检查和场景外观查看，未重新构建 Linux/Windows 安装包。

## 下载连接记录

首次 Package Manager 下载在连接 CDN 时 5 秒超时；同一网络下直连 CDN 可用。当前编辑器会话将 `assetstorev1-prd-cdn.unity3d.com` 加入 `no_proxy` / `NO_PROXY` 后，六包全部成功下载。本机缓存位于 `~/.local/share/unity3d/Asset Store-5.x`。如后续重启后再次超时，可在启动编辑器的代理例外中加入该域名。
