# Gevjon
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/RyoLee/Gevjon?include_prereleases&style=flat-square)
[![HitCount](https://hits.dwyl.com/RyoLee/Gevjon.svg?style=flat-square)](http://hits.dwyl.com/RyoLee/Gevjon)
![GitHub](https://img.shields.io/github/license/RyoLee/Gevjon?style=flat-square)
![GitHub Workflow Status (branch)](https://img.shields.io/github/workflow/status/RyoLee/Gevjon/Deploy/master?label=Build&style=flat-square)

## Core

本项目为Gevjon项目组件core组件,仅提供卡查功能

#### 界面操作

查询框输入查询内容后回车进行查询

- ID查询为准确查询
- 卡名查询为模糊查询,支持中日英三语

#### 下载方式

本项目使用Github Actions编译发布,构建完成后会自动发布到Release与[gh-pages](https://github.com/RyoLee/Gevjon/tree/gh-pages)分支,并更新jsDeliver缓存

以下4个链接正常情况下内容均一致,请在下载后检查校验和是否一致,压缩包SHA256见Release页

- [Release-Latest](https://github.com/RyoLee/Gevjon/releases/latest)

- [GH-Pages](https://github.com/RyoLee/Gevjon/raw/gh-pages/Gevjon.7z)

- [GH-Pages-CDN-JSD*](https://cdn.jsdelivr.net/gh/RyoLee/Gevjon@gh-pages/Gevjon.7z)

- [GH-Pages-CDN-CF**](https://raw.githubusercontents.com/RyoLee/Gevjon/gh-pages/Gevjon.7z)
    
    *\* jsDelivr CDN加速缓存版本,发布更新时会尝试通知更新缓存,一般与上方两个链接内容一致,但少数情况下偶尔可能被墙*

    *\*\* Cloudflare CDN加速缓存版本,可能会存在更新延迟问题,仅建议其他3种方式均无法下载时使用*

#### 控制器

[Gevjon-Observer](https://github.com/RyoLee/Gevjon-Observer)

#### 第三方调用方式

使用如下命名管道接收控制命令
```\\.\pipe\GevjonCore```

##### 控制命令格式

```json
{"id":"10000","name":"万物创世龙","mode":"name","desc":"测试文本"}
```

- mode: 查询模式
  - id: id搜索模式
  - name: 卡名搜索模式
  - issued: 控制器下发模式
- id: YGOPro版本卡片ID
- name: 卡名,支持简单的模糊搜索,比如"C107"可查询到卡片"混沌No.107 超银河眼时空龙"
- desc: 卡效文本,仅控制器下发模式生效,使用下发模式时,不会使用内部数据生成卡名,即全文显示为下发内容

可使用项目目录下PipeClient.py进行测试,请注意json需转义

```powershell
.\PipeClient.py "{\"id\":\"\",\"name\":\"107\",\"mode\":\"name\"}"
```

#### 数据更新

数据来源为[mycard/ygopro-database (github.com)](https://github.com/mycard/ygopro-database),后续更新请自行下载，将locales文件夹拖放到DB/Cover.py脚本上生成data.json数据文件

#### 风险声明

本程序(Gevjon-Core)仅提供游戏王卡片查询功能,独立运行时与游戏无关,纯手动输入0风险

至于外接了其他控制器的情况,那要看控制器是怎么实现的了,例如控制器使用OCR实现,相对内存读取实现风险会低很多(因为特征上来看和主播开OBS直播是类似的的),但是也不是0风险

不管是基于内存读取还是图像识别/OCR,**本质都是违反K社ToS的第三方软件**,因为K社官方并没有发布任何API接口或SDK(以及配套的许可文档)允许第三方开发类似的工具

**理论上检测很容易,不要以为只读不写/或者截图就安全,如果想查,反作弊程序给相关API下点钩子就能查,甚至暴力一点的会不管你的隐私信息直接用进程名的黑名单查(如TP之流)**

实际上K社管不管那100%是他们内部决定的,只不过目前这个时间点他们没管,参考DL的经验他们可能也懒得管这种非恶意利用

**总之,风险自负**

### License

[MIT License](https://github.com/RyoLee/Gevjon/blob/master/LICENSE)