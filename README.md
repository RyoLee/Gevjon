# Gevjon
[![HitCount](https://hits.dwyl.com/RyoLee/Gevjon.svg?style=flat-square)](https://github.com/RyoLee/Gevjon)
[![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/RyoLee/Gevjon?label=Release&include_prereleases&style=flat-square#?sort=date)](https://github.com/RyoLee/Gevjon/releases/latest)
[![Total Lines](https://img.shields.io/tokei/lines/github.com/RyoLee/Gevjon?label=Total%20Lines&style=flat-square)](https://github.com/RyoLee/Gevjon)
[![GitHub Issues](https://img.shields.io/github/issues/RyoLee/Gevjon?label=Issues&style=flat-square)](https://github.com/RyoLee/Gevjon/issues)
[![GitHub Workflow Status (branch)](https://img.shields.io/github/workflow/status/RyoLee/Gevjon/CI/master?label=CI&style=flat-square)](https://github.com/RyoLee/Gevjon/actions/workflows/deploy.yml)
[![Analysis](https://img.shields.io/codefactor/grade/github/RyoLee/Gevjon?label=Code%20Quality&style=flat-square)](https://www.codefactor.io/repository/github/ryolee/gevjon)
[![GitHub](https://img.shields.io/github/license/RyoLee/Gevjon?style=flat-square)](https://github.com/RyoLee/Gevjon/blob/master/LICENSE)
[![GitHub all releases](https://img.shields.io/github/downloads/RyoLee/Gevjon/total?logo=Github&style=flat-square)](https://github.com/RyoLee/Gevjon/releases/latest)
## Core

本项目为Gevjon项目组件core组件,仅提供卡查功能

#### 界面操作

查询框输入查询内容后回车(或Ctrl+回车)进行查询,支持中日英三语(包括简中官方译名和日文ruby注音)

- 模糊查询: Enter
- 准确查询: Ctrl+Enter

#### 下载方式

本项目使用Github Actions编译发布,构建完成后会自动发布到Release与[gh-pages](https://github.com/RyoLee/Gevjon/tree/gh-pages)分支

以下2个链接正常情况下内容均一致,请在下载后检查校验和是否一致,压缩包SHA256见Release页

- [Release-Latest](https://github.com/RyoLee/Gevjon/releases/latest)

- [GH-Pages](https://github.com/RyoLee/Gevjon/raw/gh-pages/Gevjon.7z)
    
    *\* 因镜像站服务条款限制和不可抗力影响,不再提供镜像下载链接,请访问github速度困难/异常地区自行百度/google如steam++等方式加速下载*

#### 控制器

[Gevjon-Observer](https://github.com/RyoLee/Gevjon-Observer)

#### 第三方调用方式

使用如下命名管道接收控制命令
```\\.\pipe\GevjonCore```

##### 控制命令格式

- mode: 查询模式
  - id: id搜索模式  //调整中
  - name: 卡名搜索模式 //调整中
  - issued: 控制器下发模式
- id: YGOPro版本卡片ID
- name: 卡名,支持简单的模糊搜索,比如"C107"可查询到卡片"混沌No.107 超银河眼时空龙"
- data: 卡片数据,仅控制器下发模式生效,使用下发模式时,不会使用内部数据生成卡名,字段类型为string,内容为
```json
{
	"cid": 11134,
	"id": 94415058,
	"cn_name": "星读之魔术师",
	"cnocg_n": "星读魔术师",
	"jp_ruby": "ほしよみのまじゅつし",
	"jp_name": "星読みの魔術師",
	"en_name": "Stargazer Magician",
	"text": {
		"types": "[怪兽|效果|灵摆] 魔法师/暗\n[★5] 1200/2400  1/1",
		"pdesc": "①：自己的灵摆怪兽进行战斗的场合，对方直到伤害步骤结束时魔法卡不能发动。\n②：另一边的自己的灵摆区域没有「魔术师」卡或者「异色眼」卡存在的场合，这张卡的灵摆刻度变成4。",
		"desc": "①：1回合1次，只让自己场上的灵摆怪兽1只因对方的效果回到自己手卡时才能发动。那1只同名怪兽从手卡特殊召唤。"
	}
}
```

可使用项目目录下PipeClient.py进行测试,请注意json需转义

```powershell
.\PipeClient.py "{\"id\":\"\",\"name\":\"107\",\"mode\":\"name\"}"
```

#### 数据更新

~~数据来源为[mycard/ygopro-database (github.com)](https://github.com/mycard/ygopro-database),后续更新请自行下载，将locales文件夹拖放到DB/Cover.py脚本上生成data.json数据文件~~

v1.3.0以后版本数据来源为[百鸽](https://ygocdb.com/), ~~由于该来源api暂未提供版本跟踪相关信息,暂不提供自动更新检查(后续视情况增加),~~ 已支持自动更新,感谢[@mercury233](https://github.com/mercury233)

#### 风险声明

本程序(Gevjon-Core)仅提供游戏王卡片查询功能,独立运行时与游戏无关,纯手动输入0风险

至于外接了其他控制器的情况,那要看控制器是怎么实现的了,例如控制器使用OCR实现,相对内存读取实现风险会低很多(因为特征上来看和主播开OBS直播是类似的的),但是也不是0风险

不管是基于内存读取还是图像识别/OCR,**本质都是违反K社ToS的第三方软件**,因为K社官方并没有发布任何API接口或SDK(以及最关键的**许可文档/文件**)允许第三方开发类似的工具

**理论上检测很容易,不要以为只读不写/或者截图就安全,如果想查,反作弊程序给相关API下点钩子就能查,甚至暴力一点的会不管你的隐私信息直接用进程名的黑名单查(如TP之流)**

实际上K社管不管那100%是他们内部决定的,只不过目前这个时间点他们没管,参考DL的经验他们可能也懒得管这种非恶意利用

**总之,风险自负**

### License

[MIT License](https://github.com/RyoLee/Gevjon/blob/master/LICENSE)
