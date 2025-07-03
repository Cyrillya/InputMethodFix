# InputMethodFix

本模组致力于解决星露谷卡输入法/输入时无法显示候选只能摸黑等问题，支持搜狗、微软、讯飞等输入法

> [!NOTE]
> This mod is specifically designed under and for Chinese environment, and therefore all official user guide or document is written in Chinese. If you are a non-Chinese speaker that also needs a IME fix, please use a translator. Generally, this mod is plug-and-play, requiring no additional adjustments.

## 简介

本人玩了一周星露谷，发现切屏回消息时要反复切换输入法，实在太麻烦。既然安慕希和泰安瑞亚的tModLoader都有自己的输入法修复Mod，我们星露谷怎么不能有？

该模组会自动检测输入状态，仅当需要文本输入（如聊天、命名动物等）时才会唤起输入法，这样即使开着中文输入法，也不用被卡输入法困扰，输入文本时也无需反复切换输入法。为了解决全屏模式下看不到系统输入法UI的问题，我在游戏中添加了一个输入法UI，只会在输入文本时显示，用于显示待输入文本和候选词，这样即使全屏也可以流畅地输入文字了。

> [!NOTE]
> 由于各种输入法五花八门，且本人能力有限，输入法UI功能只能保证支持所有符合Windows规范的输入法（因此，Linux和MacOS上也无法使用此功能）。本人测试了几款输入法，支持情况可[点击此处查看](#输入法支持情况)
> 但如果你只是想不卡输入法，即不会因为处于中文输入状态而无法走路，是可以确保支持所有输入法，以及支持Linux、MacOS、Windows平台的

这个Mod是我从零学星露谷Modding学了半天搞出来的，如果有什么地方不符合规范，望各位大佬指出。由于我在泰拉瑞亚的Modding经验丰富，本模组显示输入法框的大部分代码都是直接挪用泰拉瑞亚的，若看代码的时候看到哪些地方不太合理，估计是没删干净/没优化到。

## 输入法支持情况