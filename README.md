# Gevjon

## Core
本项目为Gevjon项目组件core组件,仅提供卡查功能
#### 界面操作

上方查询按钮使用ID准确查询,下方查询按钮使用卡名模糊查询,来源仅影响卡名模式(废话)

#### 下载方式

本项目使用Github Actions编译发布,请直接在[gh-pages](https://github.com/RyoLee/Gevjon/tree/gh-pages)分支下载



- [稳定版](https://github.com/RyoLee/Gevjon/raw/gh-pages/Gevjon.7z)
- [稳定版-CDN*](https://raw.githubusercontents.com/RyoLee/Gevjon/gh-pages/Gevjon.7z)
- [开发版](https://github.com/RyoLee/Gevjon/raw/gh-pages/Gevjon-dev.7z)
- [开发版-CDN*](https://raw.githubusercontents.com/RyoLee/Gevjon/gh-pages/Gevjon-dev.7z)

    **CDN后缀为Cloudflare CDN加速缓存版本,可能会存在更新延迟问题,仅建议国内访问阿妈粽S3服务速度过慢者使用*

#### 第三方调用方式

使用如下命名管道接收控制命令
```\\.\pipe\GevjonCore```

##### 控制命令格式 

```json
{"id":"10000","name":"万物创世龙","mode":"name"}
```

id: 卡片ID,为YGOPro版本卡片密码

name:卡名,支持简单的模糊搜索,比如英文模式下"C107"可查询到卡片"混沌No.107 超银河眼时空龙"

mode: 查询模式

- name :卡名搜索模式
- id(或其他任意值): id搜索模式

可使用项目目录下PipeClient.py进行测试,请注意json需转义

```powershell
.\PipeClient.py '{\"id\":\"\",\"name\":\"107\",\"mode\":\"name\"}'
```

三个kv均需要,无值或者无意义时传""即可,如

```json
{"id":"","name":"万物创世龙","mode":"name"}
```

(主要是懒) 后续大概会再加上一些其他的控制指令如数据源切换

#### 数据更新

数据来源为[mycard/ygopro-database (github.com)](https://github.com/mycard/ygopro-database),后续更新请自行替换locales文件夹

#### 风险声明

本程序(Gevjon-Core)仅提供游戏王卡片查询功能,与游戏无关,纯手动输入0风险(我可能有吃DMCA风险?),至于外接了其他控制器的情况,那要看控制器是怎么实现的了,如果是OCR实现,相对风险会低很多(因为特征上来看和主播开OBS直播是一样的),如果是读内存实现,**理论上很容易检测出来,不要以为只读不写就安全,一个钩子的事情而已**,实际上K社管不管那100%是他们内部决定的,

**总之,风险自负**

### License

[MIT License](https://github.com/RyoLee/Gevjon/blob/master/LICENSE)
