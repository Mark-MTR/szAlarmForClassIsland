# 深圳气象预警（ClassIsland 2 插件）

从深圳市气象台实时接口拉取气象预警信号，在主界面横排显示；同时提供**自定义规则**、**提醒**与**行动**，让预警能自动触发上课/下课提示、弹窗提醒等。

> 插件做了「数据源 / 展示」解耦：所有城市、所有预警 API 最终都映射成统一的 `AlarmItem`。想接自己城市的 API？翻到文末 [开发者教程](#开发者接入自定义-api)。

---

## 功能特性

- 📡 每 5 分钟自动刷新深圳市气象台实时预警，网络失败保留上次数据
- 🖼️ 主界面组件：图标 + 灾种 + 级别 横排显示，样式可配
- 🖱️ 悬停任一条目显示完整预警正文（满足气象台「完整传播」要求）
- 🔔 自定义规则「存在深圳气象预警」：可接到上下课提醒等触发条件里
- 🚨 提醒 + 行动：手动触发（或挂进行动组）弹出原生风格的预警弹窗，正文横向滚动
- 🎨 图标支持官方在线图标 / 本地图标两种来源，尺寸、圆角可调
- 🔌 接新城市 / 新 API 只需「复制一个文件改一改 + 加一行注册」

---

## 安装

### 方式一：`.cipx` 安装包（推荐）

1. 到 [Releases](../../releases) 下载最新的 `.cipx` 文件
2. 在 ClassIsland 主界面 →「设置 → 插件」里导入

### 方式二：手动覆盖（开发者常用）

编译后将 `bin/Release/net8.0/` 整个目录覆盖到 ClassIsland 的插件目录（通常是 `ClassIsland/Plugins/SzAlarmPlugin/`）。

---

## 使用指南

### 1. 主界面组件

在 ClassIsland 主界面编辑模式下，从组件库拖入「深圳气象预警」组件即可。

点组件上的齿轮进入设置：

| 设置项 | 说明 |
| --- | --- |
| 显示模式 | 所有预警 / 最高级别（含并列）/ 最新一条 / 每种预警中级别最高的一条 |
| 显示元素 | 分别开关「预警图标」「预警类型（如台风）」「预警级别（如橙色）」 |
| 图标来源 | 默认官方在线图标；勾选「使用本地图标」后按 icon 字段名在指定文件夹匹配 `.png` |
| 图标尺寸 / 圆角 | 12–48 px 滑块；圆角 0–24 px（0 为方角） |
| 区域过滤 | 空格分隔关键词，仅当预警地区含任一关键词才显示；留空显示全部 |
| 无预警占位 | 占位文字（默认「无预警」）与占位图片可独立开关 |

> 显示模式在「区域过滤」之后生效。

### 2. 自定义规则

插件注册了一条规则：**「存在深圳气象预警」**。

在 ClassIsland 规则集里新建规则时选择它，设置三个可选过滤条件（均为空格分隔关键词，留空表示不限）：

- **地名**：如 `南山 福田 宝安`
- **灾害种类**：如 `台风 暴雨 雷雨大风`
- **预警级别**：`白 蓝 黄 橙 红`

满足（地区 ∩ 种类 ∩ 级别）且当前有生效预警时，规则判定为真。典型用法：规则为真 → 触发「上课提醒」之类的行动。

### 3. 提醒 + 行动

插件注册了一个行动 **「深圳气象预警提醒」**。你可以把它拖进任意行动组（或由上面的规则触发）。

行动设置：

| 设置项 | 说明 |
| --- | --- |
| 播报策略 | 每次触发都播报 / 同一预警仅首次播报（去重） |
| 地名 / 种类 / 级别过滤 | 同上，决定播报哪些预警 |
| 滚动速度 | 1–20（步进 0.5），正文横向滚动速度 |
| 最大显示时长 | 10–90 秒，提醒停留上限 |
| 等待预警播报结束 | 勾选则行动会等到预警播完才继续执行后续行动 |

另外，在「设置 → 提醒」里可找到「深圳气象预警提醒」提供方，配置提醒内图标的来源（官方 / 本地）与圆角。

> ⚠️ ClassIsland 的提醒是**单窗口串行**播放的：同一条预警会排队，前一条没播完，后一条只能等。若想让预警先弹，请把它放在行动组靠前的位置，并留意当时没有别的提醒在播。

---

## 本地图标与占位图片

- **本地预警图标**：放在插件目录 `Icons/` 下，按接口的 `icon` 字段命名（例如深圳接口的 `taifengchengse.png` 表示「台风橙色」）。然后在组件设置里勾选「使用本地图标」。
- **占位图片**：任意本地 `png` / `jpg`，在组件设置里点「浏览」选择即可。

---

## 开发者：接入自定义 API

### 架构总览

插件把「取数据」和「展示」彻底分开：

```
Models/AlarmItem.cs          统一预警格式（插件内部唯一认的结构）
Services/IAlarmSource.cs     数据源接口（每个城市/API 实现一份）
Services/ShenZhenAlarmSource.cs  深圳数据源（照着抄的模板）
Services/SzAlarmService.cs   聚合器：注册所有数据源 → 合并排序 → 预下载图标
```

组件、规则、提醒、行动**只认 `AlarmItem`**，完全不感知具体接口。所以接新城市只需要做一件事：**实现一个 `IAlarmSource`，把它的原始 JSON 映射成 `AlarmItem`**。

### `AlarmItem` 字段

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `Source` | string | 数据源名（城市/来源），如 `深圳` |
| `Area` | string | 预警地区，如 `全市`、`福田区` |
| `Type` | string | 灾害种类，如 `台风`、`暴雨` |
| `Color` | string | 预警级别，如 `蓝色`、`橙色` |
| `Text` | string | 预警正文（滚动显示 + 语音播报） |
| `FullName` | string | 完整预警名（可选），兼容港澳「三号强风信号」等；留空回退 `{Type}{Color}预警` |
| `IssuerName` | string | 预警发布方名称，如 `深圳市气象台`；数据源未提供时自动回退为 `预警方` |
| `IconName` | string | 图标名（无扩展名），本地 `.png` 匹配用，如 `taifengchengse` |
| `IconUrl` | string | 图标完整地址（远程官方图标 URL，自动下载缓存） |
| `PublishedAt` | DateTime? | 发布时间（可选，「最新一条」排序用） |
| `Order` | int | 源内优先级（可选，越小越优先） |
| `Level` | int | 只读，级别权重（白0 蓝1 黄2 橙3 红4），由 `Color` 自动算出 |

级别归一化统一走 `AlarmLevels.Of()` / `AlarmLevels.CoreColor()`，别自己写 `switch`。

### 三步接入

**第 1 步**：复制 `Services/ShenZhenAlarmSource.cs`，改名，比如 `GuangZhouAlarmSource.cs`。

**第 2 步**：改两处——接口地址，以及 `ToUnified()` 里的字段映射（把广州接口的字段塞进 `AlarmItem`）。原始 DTO 就定义在这个文件内部，不用暴露出去。

**第 3 步**：在 `Services/SzAlarmService.cs` 构造函数里注册一行：

```csharp
_sources = new List<IAlarmSource>
{
    new ShenZhenAlarmSource(),
    new GuangZhouAlarmSource(),   // ← 加这一行
};
```

完事。组件、规则、提醒、行动会自动合并展示所有已注册数据源的预警。

### 完整示例

假设接一个「广州」的预警接口，返回 JSON 形如：

```json
{
  "alarms": [
    {
      "area": "天河区",
      "type": "暴雨",
      "level": "橙色",
      "content": "【广州】预计未来 3 小时……",
      "icon": "baoyuchengse",
      "time": "2026-10-15 16:00:00"
    }
  ]
}
```

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SzAlarmPlugin.Models;

namespace SzAlarmPlugin.Services;

/// <summary>广州气象预警数据源：拉取接口并映射为统一 AlarmItem。</summary>
public class GuangZhouAlarmSource : IAlarmSource
{
    public string Name => "广州";

    private static readonly Uri ApiUri = new("https://example.com/gz/alarm.json");

    // 图标基址：完整地址 = 基址 + icon 字段名 + ".png"
    private const string IconBase = "https://example.com/gz/icon/";

    private readonly HttpClient _http = new();

    public async Task<IReadOnlyList<AlarmItem>> FetchAsync()
    {
        var json = await _http.GetStringAsync(ApiUri);
        var root = JsonSerializer.Deserialize<GzRoot>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (root?.Alarms is null) return Array.Empty<AlarmItem>();

        var list = new List<AlarmItem>();
        foreach (var a in root.Alarms)
        {
            var icon = a.Icon ?? "";
            list.Add(new AlarmItem
            {
                Source = "广州",
                Area = a.Area ?? "",
                Type = a.Type ?? "",
                Color = a.Level ?? "",
                Text = a.Content ?? "",
                IssuerName = "广州市气象台",
                IconName = icon,
                IconUrl = string.IsNullOrEmpty(icon) ? "" : IconBase + icon + ".png",
                PublishedAt = DateTime.TryParse(a.Time, out var dt) ? dt : null,
                Order = 0
            });
        }
        return list;
    }

    // ---- 广州接口原始结构（仅本数据源内部使用） ----
    private class GzRoot { public List<GzAlarm>? Alarms { get; set; } }

    private class GzAlarm
    {
        public string? Area { get; set; }
        public string? Type { get; set; }
        public string? Level { get; set; }
        public string? Content { get; set; }
        public string? Icon { get; set; }
        public string? Time { get; set; }
    }
}
```

要点：

1. **`FetchAsync` 只负责取数 + 映射**，异常不用处理（上层会捕获并跳过该源）。
2. **`IconUrl` 一定要给完整地址**，否则远程图标显示不出来；本地图标则靠 `IconName` 匹配。
3. **级别字段**填「白/蓝/黄/橙/红」或含这些字（如「橙色预警」），`Level` 权重和规则过滤才能正确工作。
4. **`IssuerName`**（发布方）一定要填，遮罩横幅会显示「XX 发布 台风蓝色预警」；留空则回退为「预警方」。
5. **`FullName`**（完整预警名）用于大陆模板表达不了的信号体系。港澳的「三号强风信号」「山泥倾泻警告」这类，把 `FullName` 填上，横幅与弹窗标题就会原样显示，不再硬套「灾种 + 级别 + 预警」。

### 港澳等信号体系怎么接（FullName）

港澳预警走的是「信号 / 警告」命名，跟大陆「灾种 + 级别 + 预警」对不上，例如：

- 三号强风信号、一号戒备信号（热带气旋警告信号）
- 山泥倾泻警告、雷暴警告、暴雨警告信号

这类信号没有「台风蓝色」这种颜色级别，硬套模板会拼出「热带气旋预警」之类的怪名。此时**只要把 `FullName` 填上**，横幅与弹窗标题就会原样显示 `FullName`：

```csharp
list.Add(new AlarmItem
{
    Source = "香港",
    Area = a.Area ?? "",
    Type = a.Type ?? "",          // 可填「热带气旋」「山泥倾泻」等，用于种类过滤
    Color = a.Level ?? "",        // 无对应颜色级别就留空（不参与级别过滤/排序）
    FullName = a.Signal ?? "",    // ← 关键：如「三号强风信号」
    IssuerName = "香港天文台",
    Text = a.Content ?? "",
    IconName = icon,
    IconUrl = IconBase + icon + ".png",
});
```

说明：

- `FullName` 填了就**优先显示**；留空才回退到「`{Type}{Color}预警`」的大陆模板。
- 去重（播报策略「同一预警仅首次」）会把 `DisplayName` 纳入判断，所以「三号强风信号」和「八号烈风信号」即使 `Type`/`Color`/`Area` 相同也不会被误判成同一条。
- `Type`、`Color` 仍照常参与规则/行动的「种类」「级别」过滤，`Color` 留空即视为无级别。

---

## 编译与打包

1. 安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 克隆本仓库后执行：

```bash
dotnet build -c Release
```

3. 产物在 `bin/Release/net8.0/`，其中 `.cipx` 可直接导入 ClassIsland；或把整个目录覆盖到插件目录。

---

## 数据来源

- 数据接口：`https://weather.121.com.cn/data_cache/szWeather/alarm/szAlarm.json`
- 官方图标：`https://weather.sz.gov.cn/alarmIcon/{icon}.png`（`icon` 为接口字段名，如 `taifengchengse`）

预警信息版权归深圳市气象台所有，本插件仅做展示。
