# VikunjaHook - Vikunja Webhook 处理服务

这是一个支持AOT编译的Vikunja webhook处理服务，使用.NET 10和原生JSON序列化，无需额外依赖。

## 功能特性

- ✅ 支持所有Vikunja webhook事件类型（26种事件）
- ✅ 原生AOT编译支持
- ✅ 零外部依赖
- ✅ 使用System.Text.Json源生成器
- ✅ 完整的类型安全

## 支持的事件类型

### 任务事件
- `task.created` - 任务创建
- `task.updated` - 任务更新
- `task.deleted` - 任务删除

### 项目事件
- `project.created` - 项目创建
- `project.updated` - 项目更新
- `project.deleted` - 项目删除

### 任务分配事件
- `task.assignee.created` - 任务分配给用户
- `task.assignee.deleted` - 取消任务分配

### 评论事件
- `task.comment.created` - 评论创建
- `task.comment.updated` - 评论更新
- `task.comment.deleted` - 评论删除

### 附件事件
- `task.attachment.created` - 附件创建
- `task.attachment.deleted` - 附件删除

### 任务关系事件
- `task.relation.created` - 任务关系创建
- `task.relation.deleted` - 任务关系删除

### 标签事件
- `label.created` - 标签创建
- `label.updated` - 标签更新
- `label.deleted` - 标签删除
- `task.label.created` - 任务添加标签
- `task.label.deleted` - 任务移除标签

### 用户事件
- `user.created` - 用户创建

### 团队事件
- `team.created` - 团队创建
- `team.updated` - 团队更新
- `team.deleted` - 团队删除
- `team.member.added` - 团队成员添加
- `team.member.removed` - 团队成员移除

## API端点

### POST /webhook/vikunja
接收Vikunja webhook事件

**请求示例:**
```json
{
  "event_name": "task.created",
  "time": "2026-01-26T10:00:00Z",
  "data": {
    "task": {
      "id": 123,
      "title": "新任务",
      "description": "任务描述",
      "done": false,
      "project_id": 1
    }
  }
}
```

### GET /webhook/vikunja/events
获取所有支持的事件类型列表

### GET /health
健康检查端点

## 使用方法

### 1. 运行服务
```bash
cd src/VikunjaHook/VikunjaHook
dotnet run
```

### 2. 在Vikunja中配置webhook
在Vikunja的项目设置中添加webhook URL:
```
http://your-server:5000/webhook/vikunja
```

### 3. 自定义处理逻辑
创建自己的webhook处理器，实现`IWebhookHandler`接口:

```csharp
public class CustomWebhookHandler : IWebhookHandler
{
    public async Task HandleWebhookAsync(VikunjaWebhookPayload payload)
    {
        // 你的自定义逻辑
    }
}
```

然后在Program.cs中注册:
```csharp
builder.Services.AddSingleton<IWebhookHandler, CustomWebhookHandler>();
```

## AOT发布

```bash
dotnet publish -c Release
```

生成的可执行文件将是原生编译的，启动速度快，内存占用小。

## 项目结构

```
VikunjaHook/
├── Models/
│   ├── VikunjaEventTypes.cs      # 事件类型常量
│   └── VikunjaWebhookPayload.cs  # 数据模型
├── Services/
│   ├── IWebhookHandler.cs        # 处理器接口
│   └── DefaultWebhookHandler.cs  # 默认处理器实现
├── AppJsonSerializerContext.cs   # JSON序列化上下文（AOT）
└── Program.cs                     # 主程序
```

## 技术栈

- .NET 10
- ASP.NET Core Minimal API
- System.Text.Json (源生成器)
- Native AOT
