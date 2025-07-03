<h1 align="center">星露谷输入法修复 Input Method Fix</h1>

<div align="center">

📥 [Nexus下载]() | 📥 [Github下载](https://github.com/Cyrillya/InputMethodFix/releases/latest)

本模组致力于解决星露谷卡输入法/输入时无法显示候选只能摸黑等问题
支持搜狗、微软、讯飞等输入法

</div>

> [!NOTE]
> This mod is specifically designed under and for Chinese environment, and therefore all official user guide or document is written in Chinese. If you are a non-Chinese speaker that also needs a IME fix, please use a translator. Generally, this mod is plug-and-play, requiring no additional adjustments.

## 📖 简介

本人玩了一周星露谷，发现切屏回消息时要反复切换输入法，实在太麻烦。既然安慕希和泰安瑞亚的tModLoader都有自己的输入法修复Mod，我们星露谷怎么不能有？

该模组会自动检测输入状态，仅当需要文本输入（如聊天、命名动物等）时才会唤起输入法，这样即使开着中文输入法，也不用被卡输入法困扰，输入文本时也无需反复切换输入法。为了解决全屏模式下看不到系统输入法UI的问题，我在游戏中添加了一个输入法UI，只会在输入文本时显示，用于显示待输入文本和候选词，这样即使全屏也可以流畅地输入文字了。

> [!NOTE]
> 由于各种输入法五花八门，且本人能力有限，输入法UI功能只能保证支持所有符合Windows规范的输入法（因此，Linux和MacOS上也无法使用此功能）。本人测试了几款输入法，支持情况可[点击此处查看](#输入法支持情况)
> 但如果你只是想不卡输入法，即不会因为处于中文输入状态而无法走路，是可以确保支持所有输入法，以及支持Linux、MacOS、Windows平台的

这个Mod是我从零学星露谷Modding学了半天搞出来的，如果有什么地方不符合规范，望各位大佬指出。由于我在泰拉瑞亚的Modding经验丰富，本模组显示输入法框的大部分代码都是直接挪用泰拉瑞亚的，若看代码的时候看到哪些地方不太合理，估计是没删干净/没优化到。

⭐ 如果觉得好用，烦请在[Github](https://github.com/Cyrillya/InputMethodFix)上点个Star！

## 📊 输入法支持情况

以下是我测试的几款输入法的支持情况，如果你测试了其他输入法的情况，或提出下表中的错误，可以在[Github](https://github.com/Cyrillya/InputMethodFix)上开启[Pull Request](https://github.com/Cyrillya/InputMethodFix/pulls)以修改

| 输入法 | 修复卡输入法 | 修复输入法不显示 | 备注       |
| ------ | ------------ | ------------------ | ---------- |
| 微软拼音 | ✅           | ✅                 | 完全支持   |
| 搜狗拼音 | ✅           | ✅                 | 窗口模式下，系统输入法框会一直显示 |
| 百度    | ✅           | ❌                 | 游戏内输入法只能显示输入栏，不能显示候选词 |
| QQ拼音  | ✅           | ✅                 | 完全支持   |
| 讯飞    | ✅           | ✅                 | 完全支持   |
| 微信    | ✅           | ❌                 | 有时能用有时不能用，不建议使用 |

此外，尽量不要在游戏中切换输入法，有概率出BUG，实在是不会修了😅