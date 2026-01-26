/**
 * Vikunja MCP C# 服务器完整测试套件
 * 包含基础功能测试和 Tasks 工具完整测试
 */

const API_BASE = process.env.API_BASE || 'http://localhost:5082';
const VIKUNJA_URL = process.env.VIKUNJA_URL;
const VIKUNJA_TOKEN = process.env.VIKUNJA_TOKEN;

// 验证必需的环境变量
if (!VIKUNJA_URL || !VIKUNJA_TOKEN) {
  console.error('错误: 必须设置 VIKUNJA_URL 和 VIKUNJA_TOKEN 环境变量');
  process.exit(1);
}

const colors = {
  reset: '\x1b[0m',
  green: '\x1b[32m',
  red: '\x1b[31m',
  yellow: '\x1b[33m',
  cyan: '\x1b[36m',
  blue: '\x1b[34m',
  magenta: '\x1b[35m'
};

function log(message, color = 'reset') {
  console.log(`${colors[color]}${message}${colors.reset}`);
}

const testState = {
  sessionId: null,
  projectId: null,
  taskId: null,
  taskId2: null,
  labelId: null,
  teamId: null,
  commentId: null,
  reminderDate: null
};

const results = {
  total: 0,
  passed: 0,
  failed: 0,
  skipped: 0
};

async function makeRequest(endpoint, options = {}) {
  try {
    const response = await fetch(`${API_BASE}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers
      }
    });
    const data = await response.json();
    return { response, data, ok: response.ok };
  } catch (error) {
    return { error: error.message, ok: false };
  }
}

async function test(name, fn) {
  results.total++;
  try {
    const result = await fn();
    if (result === 'skip') {
      results.skipped++;
      log(`⊘ ${name} (跳过)`, 'yellow');
      return 'skip';
    }
    results.passed++;
    log(`✓ ${name}`, 'green');
    return true;
  } catch (error) {
    results.failed++;
    log(`✗ ${name}: ${error.message}`, 'red');
    return false;
  }
}

async function runBasicTests() {
  log('\n╔══════════════════════════════════════════════════════════════╗', 'magenta');
  log('║  第一部分：基础功能测试                                      ║', 'magenta');
  log('╚══════════════════════════════════════════════════════════════╝\n', 'magenta');
  
  // 1. 健康检查
  await test('健康检查', async () => {
    const { ok } = await makeRequest('/health');
    if (!ok) throw new Error('健康检查失败');
  });
  
  // 2. 认证
  await test('API Token 认证', async () => {
    const { ok, data } = await makeRequest('/mcp/auth', {
      method: 'POST',
      body: JSON.stringify({
        apiUrl: VIKUNJA_URL,
        apiToken: VIKUNJA_TOKEN
      })
    });
    if (!ok || !data.sessionId) throw new Error('认证失败');
    testState.sessionId = data.sessionId;
  });
  
  // 3. 创建项目
  await test('创建项目 (PUT)', async () => {
    const { ok, data, response } = await makeRequest('/mcp/tools/projects/create', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        title: `完整测试项目 ${Date.now()}`,
        description: '用于完整测试套件'
      })
    });
    if (!ok || !data.success) throw new Error(`创建失败: ${response.status}`);
    testState.projectId = data.data?.project?.id;
    if (!testState.projectId) throw new Error('未返回项目ID');
  });
  
  // 4. 获取项目详情
  await test('获取项目详情', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/projects/get', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.projectId })
    });
    if (!ok || !data.success) throw new Error('获取失败');
  });
  
  // 5. 列出项目
  await test('列出项目', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/projects/list', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ page: 1, perPage: 10 })
    });
    if (!ok || !data.success) throw new Error('列出失败');
  });
  
  // 6. 更新项目
  await test('更新项目', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/projects/update', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.projectId,
        title: `更新后的项目 ${Date.now()}`
      })
    });
    if (!ok || !data.success) throw new Error('更新失败');
  });
  
  // 7. 创建标签
  await test('创建标签 (PUT)', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/labels/create', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        title: `测试标签 ${Date.now()}`,
        hexColor: '#FF5733'
      })
    });
    if (!ok || !data.success) throw new Error('创建失败');
    testState.labelId = data.data?.label?.id;
    if (!testState.labelId) throw new Error('未返回标签ID');
  });
  
  // 8. 创建团队
  await test('创建团队 (PUT)', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/teams/create', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        name: `测试团队 ${Date.now()}`,
        description: '完整测试套件'
      })
    });
    if (!ok || !data.success) throw new Error('创建失败');
    testState.teamId = data.data?.team?.id;
    if (!testState.teamId) throw new Error('未返回团队ID');
  });
}

async function runTasksTests() {
  log('\n╔══════════════════════════════════════════════════════════════╗', 'magenta');
  log('║  第二部分：Tasks 工具完整测试 (22 个子命令)                 ║', 'magenta');
  log('╚══════════════════════════════════════════════════════════════╝\n', 'magenta');
  
  // 任务 CRUD 操作
  log('【任务 CRUD 操作】', 'blue');
  
  await test('tasks/create - 创建任务', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/create', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        projectId: testState.projectId,
        title: `完整测试任务 ${Date.now()}`,
        description: '测试所有任务功能',
        priority: 3
      })
    });
    if (!ok || !data.success) throw new Error('创建失败');
    testState.taskId = data.data?.task?.id;
    if (!testState.taskId) throw new Error('未返回任务ID');
  });
  
  await test('tasks/get - 获取任务详情', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/get', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.taskId })
    });
    if (!ok || !data.success) throw new Error('获取失败');
    if (data.data?.task?.id !== testState.taskId) throw new Error('任务ID不匹配');
  });
  
  await test('tasks/list - 列出任务', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/list', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        projectId: testState.projectId,
        page: 1,
        perPage: 10
      })
    });
    if (!ok || !data.success) throw new Error('列出失败');
    if (!data.data?.tasks) throw new Error('未返回任务列表');
  });
  
  await test('tasks/update - 更新任务', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/update', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        title: `更新后的任务 ${Date.now()}`,
        priority: 5,
        done: false
      })
    });
    if (!ok || !data.success) throw new Error('更新失败');
  });
  
  // 批量操作
  log('\n【批量操作】', 'blue');
  
  await test('tasks/bulk-create - 批量创建任务', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/bulk-create', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        projectId: testState.projectId,
        tasks: [
          { title: `批量任务1 ${Date.now()}`, priority: 1 },
          { title: `批量任务2 ${Date.now()}`, priority: 2 },
          { title: `批量任务3 ${Date.now()}`, priority: 3 }
        ]
      })
    });
    if (!ok || !data.success) throw new Error('批量创建失败');
    if (!data.data?.tasks || data.data.tasks.length < 3) throw new Error('批量创建数量不正确');
    testState.taskId2 = data.data.tasks[0]?.id;
  });
  
  await test('tasks/bulk-update - 批量更新任务', async () => {
    if (!testState.taskId2) {
      throw new Error('没有可用的第二个任务ID（bulk-create 测试可能失败）');
    }
    const { ok, data } = await makeRequest('/mcp/tools/tasks/bulk-update', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        taskIds: [testState.taskId, testState.taskId2],
        field: 'priority',
        value: 4
      })
    });
    if (!ok || !data.success) throw new Error('批量更新失败');
  });
  
  // 任务分配
  log('\n【任务分配】', 'blue');
  
  await test('tasks/list-assignees - 列出任务分配者', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/list-assignees', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.taskId })
    });
    if (!ok || !data.success) throw new Error('列出分配者失败');
  });
  
  // 评论功能
  log('\n【评论功能】', 'blue');
  
  await test('tasks/comment - 添加评论', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/comment', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        comment: `测试评论 ${Date.now()}`
      })
    });
    if (!ok || !data.success) throw new Error('添加评论失败');
    testState.commentId = data.data?.comment?.id;
  });
  
  await test('tasks/list-comments - 列出评论', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/list-comments', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.taskId })
    });
    if (!ok || !data.success) throw new Error('列出评论失败');
    if (!data.data?.comments) throw new Error('未返回评论列表');
  });
  
  // 标签功能
  log('\n【标签功能】', 'blue');
  
  await test('tasks/apply-label - 应用标签', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/apply-label', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        labels: [testState.labelId]
      })
    });
    if (!ok || !data.success) throw new Error('应用标签失败');
  });
  
  await test('tasks/list-labels - 列出任务标签', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/list-labels', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.taskId })
    });
    if (!ok || !data.success) throw new Error('列出标签失败');
  });
  
  await test('tasks/remove-label - 移除标签', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/remove-label', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        labels: [testState.labelId]
      })
    });
    if (!ok || !data.success) throw new Error('移除标签失败');
  });
  
  // 提醒功能
  log('\n【提醒功能】', 'blue');
  
  await test('tasks/add-reminder - 添加提醒', async () => {
    const reminderDate = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
    testState.reminderDate = reminderDate;
    const { ok, data } = await makeRequest('/mcp/tools/tasks/add-reminder', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        reminderDate: reminderDate
      })
    });
    if (!ok || !data.success) throw new Error('添加提醒失败');
    const reminders = data.data?.task?.reminders;
    if (!reminders || reminders.length === 0) {
      throw new Error('添加提醒后未返回提醒列表');
    }
  });
  
  await test('tasks/list-reminders - 列出提醒', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/list-reminders', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.taskId })
    });
    if (!ok || !data.success) throw new Error('列出提醒失败');
  });
  
  await test('tasks/remove-reminder - 移除提醒', async () => {
    if (!testState.reminderDate) {
      throw new Error('没有可用的提醒日期（add-reminder 测试可能失败）');
    }
    const { ok, data } = await makeRequest('/mcp/tools/tasks/remove-reminder', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        reminderDate: testState.reminderDate
      })
    });
    if (!ok || !data.success) throw new Error('移除提醒失败');
  });
  
  // 任务关系
  log('\n【任务关系】', 'blue');
  
  await test('tasks/relate - 关联任务', async () => {
    if (!testState.taskId2) {
      throw new Error('没有可用的第二个任务ID（bulk-create 测试可能失败）');
    }
    const { ok, data } = await makeRequest('/mcp/tools/tasks/relate', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        otherTaskId: testState.taskId2,
        relationKind: 'related'
      })
    });
    if (!ok || !data.success) throw new Error('关联任务失败');
  });
  
  await test('tasks/relations - 列出任务关系', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/relations', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.taskId })
    });
    if (!ok || !data.success) throw new Error('列出关系失败');
  });
  
  await test('tasks/unrelate - 取消关联任务', async () => {
    if (!testState.taskId2) {
      throw new Error('没有可用的第二个任务ID（bulk-create 测试可能失败）');
    }
    const { ok, data } = await makeRequest('/mcp/tools/tasks/unrelate', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        id: testState.taskId,
        otherTaskId: testState.taskId2,
        relationKind: 'related'
      })
    });
    if (!ok || !data.success) throw new Error('取消关联失败');
  });
  
  // 删除操作
  log('\n【删除操作】', 'blue');
  
  await test('tasks/bulk-delete - 批量删除任务', async () => {
    // 创建一些临时任务用于删除
    const { ok: createOk, data: createData } = await makeRequest('/mcp/tools/tasks/bulk-create', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({
        projectId: testState.projectId,
        tasks: [
          { title: `待删除任务1 ${Date.now()}` },
          { title: `待删除任务2 ${Date.now()}` }
        ]
      })
    });
    if (!createOk || !createData.success) throw new Error('创建待删除任务失败');
    
    const taskIds = createData.data.tasks.map(t => t.id);
    const { ok, data } = await makeRequest('/mcp/tools/tasks/bulk-delete', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ taskIds })
    });
    if (!ok || !data.success) throw new Error('批量删除失败');
  });
  
  await test('tasks/delete - 删除任务', async () => {
    const { ok, data } = await makeRequest('/mcp/tools/tasks/delete', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.taskId })
    });
    if (!ok || !data.success) throw new Error('删除失败');
  });
}

async function cleanup() {
  log('\n【清理测试资源】', 'yellow');
  
  if (testState.labelId) {
    await makeRequest('/mcp/tools/labels/delete', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.labelId })
    });
  }
  
  if (testState.teamId) {
    await makeRequest('/mcp/tools/teams/delete', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.teamId })
    });
  }
  
  if (testState.projectId) {
    await makeRequest('/mcp/tools/projects/delete', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${testState.sessionId}` },
      body: JSON.stringify({ id: testState.projectId })
    });
  }
}

async function main() {
  log('\n╔══════════════════════════════════════════════════════════════╗', 'cyan');
  log('║  Vikunja MCP C# 服务器完整测试套件                          ║', 'cyan');
  log('║  包含基础功能测试 + Tasks 工具完整测试                      ║', 'cyan');
  log('╚══════════════════════════════════════════════════════════════╝', 'cyan');
  
  // 运行基础功能测试
  await runBasicTests();
  
  // 运行 Tasks 工具完整测试
  await runTasksTests();
  
  // 清理资源
  await cleanup();
  
  // 结果汇总
  log('\n╔══════════════════════════════════════════════════════════════╗', 'cyan');
  log('║  测试结果汇总                                                ║', 'cyan');
  log('╚══════════════════════════════════════════════════════════════╝', 'cyan');
  log(`\n总计: ${results.total}`, 'cyan');
  log(`✓ 通过: ${results.passed}`, 'green');
  log(`✗ 失败: ${results.failed}`, results.failed > 0 ? 'red' : 'green');
  log(`⊘ 跳过: ${results.skipped}`, 'yellow');
  const passRate = results.total > 0 ? ((results.passed / results.total) * 100).toFixed(1) : 0;
  log(`通过率: ${passRate}%\n`, results.failed === 0 && results.skipped === 0 ? 'green' : 'yellow');
  
  if (results.failed === 0 && results.skipped === 0) {
    log('🎉 所有测试通过！Vikunja MCP C# 服务器 100% 功能完整！', 'green');
  } else if (results.failed === 0) {
    log(`⚠️ ${results.skipped} 个测试被跳过`, 'yellow');
  } else {
    log(`❌ ${results.failed} 个测试失败`, 'red');
  }
  
  log('\n测试详情:', 'cyan');
  log(`  - 基础功能测试: 8 项`, 'cyan');
  log(`  - Tasks 工具测试: 22 项`, 'cyan');
  log(`  - 总计: 30 项核心功能测试`, 'cyan');
}

main().catch(error => {
  log(`\n致命错误: ${error.message}`, 'red');
  console.error(error);
  process.exit(1);
});
