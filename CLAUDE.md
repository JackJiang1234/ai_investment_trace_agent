# CLAUDE.md

个人股票 AI 追踪 Agent —— 按配置的 A股/港股代码，每交易日收盘后自动生成追踪报告。

> 详细需求见 `docs/requirements.md`。本文件是给 Claude Code 的工作指引，需求变更时同步更新此文件。

## 开发方法：TDD（强制）

本项目采用测试驱动开发，**红-绿-重构**循环，严格遵守：

1. **先写测试**：任何生产代码前，先写一个会失败的单元测试（Red）。
2. **最小实现**：只写让该测试通过的最少代码（Green）。
3. **重构**：在测试保护下清理代码（Refactor）。
4. 未看到测试失败过，不认为测试有效。
5. 提交前 `dotnet test` 必须全绿。

**测试要点**：
- 数据源等外部 HTTP 依赖必须通过接口抽象 + Mock 测试，**单元测试不发真实网络请求**。
- 用真实抓取的接口响应样本存为测试 fixture（`tests/**/Fixtures/`），驱动解析器测试。
- 指标计算（MA、涨跌幅、量比、阈值判定、关键词命中）是纯函数，优先覆盖，边界用例齐全。
- 测试框架：xUnit + FluentAssertions + NSubstitute（Mock）。

## 技术栈

- **.NET 10 (LTS)**，Console App
- `HttpClient` + `System.Text.Json` 采集数据（无 akshare/efinance 等 Python 库等价物，直接调公开 HTTP 接口自行解析）
- `appsettings.json` 配置（`Microsoft.Extensions.Configuration` 绑定强类型 Options）
- **Scriban** 模板引擎 → 生成 Markdown + HTML 报告
- 指标计算：纯 C#，无三方依赖
- 运行形态：**GitHub Actions cron**（非本地计划任务），报告 `git commit` 回仓库

## 解决方案结构

```
AiInvestmentTraceAgent.sln
├── src/
│   ├── Agent.Console/         # 入口、编排、appsettings.json、DI 组装
│   ├── Agent.Core/            # 领域模型、指标计算、报告编排、接口定义
│   ├── Agent.DataSource/      # 行情/公告/财报/新闻 HttpClient 封装 + 解析
│   ├── Agent.Reporting/       # Scriban 模板 → Markdown/HTML
│   ├── Agent.Delivery/        # IReportDelivery：File(默认) / 企业微信(预留)
│   └── Agent.Summarizer/      # ISummarizer：NoOp(默认) / LLM(预留)
└── tests/
    └── Agent.Core.Tests/      # 及各模块对应测试工程
```

**依赖方向**：`Console → Core ← DataSource/Reporting/Delivery/Summarizer`。`Core` 定义接口，不依赖具体实现；外层实现 `Core` 的接口，由 `Console` 做 DI 组装。

## 功能范围（本期）

- ✅ 行情走势追踪（**不含持仓盈亏**）+ 阈值提醒（涨跌幅、量比）
- ✅ 公告追踪：重要类型过滤，纯规则
- ✅ 财报追踪：披露提醒 + 关键财务数据 + 业绩预告/快报
- ✅ 风险消息：关键词命中高亮，纯规则、不解读内容
- ✅ 本地文件输出（HTML + Markdown）→ Actions 提交回仓库

**明确不做**：持仓盈亏、买卖建议、盘中实时监控。

## 关键决策（不要擅自更改，需先与用户确认）

- **AI 层默认关闭**：`ISummarizer` 预留但默认 `NoOpSummarizer`。本期所有功能为纯规则/关键词，**零大模型成本**。启用 AI（读懂公告/风险分级）需显式开启，届时用 `claude-haiku-4-5`。
- **数据源**：东方财富 push2（行情/资金主源）、新浪（备源）、**东财统一公告 API（A股公告，np-anotice-stock）**、港交所披露易 hkexnews（港股公告，M5b 待接）、东财财务接口（财报）。
  - 注：A股公告原定巨潮 cninfo，实测东财统一公告 API 更简洁一致且海外可达，已改用东财；东财该接口不返回港股，港股公告留 M5b 经 hkexnews 补齐。
- **调度**：GitHub Actions，统一 **09:00 UTC（北京 17:00）** 收盘后触发一次（cron 为 UTC 且高峰期会延迟，已留缓冲）。
- **投递**：默认写文件；企业微信/Server酱推送为预留接口。

## 编码约定

- 配置与逻辑分离，**无硬编码本地路径**（便于将来迁云）。
- 所有网络请求：超时 + 重试 + 备用源降级 + 结构化日志。
- 数据源接口变更/反爬风险高 → 解析层容错，字段缺失不崩溃，记录并跳过。
- 股票代码统一格式 `600519.SH` / `00700.HK`，在边界处解析市场与纯代码。
- 命名清晰；仅在意图不明显处加注释；生产路径必须有错误处理。
- 绝不在代码/示例中写入真实密钥或凭证，用占位符；Webhook/API Key 走 GitHub Secrets。

## 常用命令

```bash
dotnet test                              # 跑全部测试（提交前必过）
dotnet test --filter FullyQualifiedName~Indicators   # 跑指定测试
dotnet run --project src/Agent.Console   # 本地生成一次报告
dotnet build                             # 构建
```

## 已知风险 / 首要验证

- **海外 GitHub runner 访问中国行情/公告接口可能限流或失败**。
- 👉 **实现第一步**：先做最小连通性验证——一个只取 1 只 A股 + 1 只港股行情/公告的小程序，在 Actions 上实跑确认能取到数据，再搭完整框架。避免搭好一切才发现云端取不到数。
