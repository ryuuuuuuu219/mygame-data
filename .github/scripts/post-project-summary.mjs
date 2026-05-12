import { execFileSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";

const openaiApiKey = process.env.OPENAI_API_KEY;
const discordWebhookUrl = process.env.DISCORD_WEBHOOK_URL;
const model = process.env.OPENAI_MODEL || "gpt-5.4-mini";
const extraContext = process.env.EXTRA_CONTEXT || "";

if (!openaiApiKey) {
  throw new Error("OPENAI_API_KEY is not set.");
}

if (!discordWebhookUrl) {
  throw new Error("DISCORD_WEBHOOK_URL is not set.");
}

const contextFiles = [
  "memo/README.md",
  "memo/ROADMAP.md",
  "memo/タスク一覧/進捗確認.md",
  "memo/タスク一覧/整理後/ロードマップ.md",
  "document/README.md",
  "document/ユーザー向け/README.md",
];

function runGit(args) {
  try {
    return execFileSync("git", args, { encoding: "utf8" }).trim();
  } catch {
    return "";
  }
}

function readContextFile(path) {
  if (!existsSync(path)) {
    return "";
  }

  const text = readFileSync(path, "utf8").trim();
  if (!text) {
    return "";
  }

  return `\n\n--- ${path} ---\n${text.slice(0, 12000)}`;
}

function extractOutputText(response) {
  if (typeof response.output_text === "string") {
    return response.output_text.trim();
  }

  const chunks = [];
  for (const item of response.output || []) {
    for (const content of item.content || []) {
      if (typeof content.text === "string") {
        chunks.push(content.text);
      }
    }
  }

  return chunks.join("\n").trim();
}

function truncateForDiscord(text) {
  if (text.length <= 1900) {
    return text;
  }

  return `${text.slice(0, 1850).trimEnd()}\n\n...`;
}

const commitTitle = runGit(["log", "-1", "--pretty=%s"]);
const commitHash = runGit(["rev-parse", "--short", "HEAD"]);
const commitBody = runGit(["log", "-1", "--pretty=%b"]);
const changedStat =
  runGit(["diff", "--stat", "HEAD~1", "HEAD"]) ||
  runGit(["show", "--stat", "--oneline", "--no-renames", "HEAD"]);

const repo = process.env.GITHUB_REPOSITORY || "";
const runUrl =
  process.env.GITHUB_SERVER_URL && repo && process.env.GITHUB_RUN_ID
    ? `${process.env.GITHUB_SERVER_URL}/${repo}/actions/runs/${process.env.GITHUB_RUN_ID}`
    : "";

const context = [
  `Repository: ${repo || "local"}`,
  `Commit: ${commitHash} ${commitTitle}`,
  commitBody ? `Commit body:\n${commitBody}` : "",
  changedStat ? `Changed files:\n${changedStat}` : "",
  extraContext ? `Extra context:\n${extraContext}` : "",
  ...contextFiles.map(readContextFile),
]
  .filter(Boolean)
  .join("\n");

const instructions = [
  "あなたは日本語で開発状況を共有するプロジェクト秘書です。",
  "Discordにそのまま投稿できるMarkdownで、1800文字以内にまとめてください。",
  "誇張せず、未完了部分は未完了と書いてください。",
  "構成は「今日の共有」「進んだこと」「次に見ること」を基本にしてください。",
  "メンション、@everyone、@here、過度な絵文字は使わないでください。",
].join("\n");

const openaiResponse = await fetch("https://api.openai.com/v1/responses", {
  method: "POST",
  headers: {
    Authorization: `Bearer ${openaiApiKey}`,
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    model,
    instructions,
    input: context,
    max_output_tokens: 900,
    store: false,
  }),
});

if (!openaiResponse.ok) {
  const errorBody = await openaiResponse.text();
  throw new Error(`OpenAI request failed: ${openaiResponse.status} ${errorBody}`);
}

const openaiJson = await openaiResponse.json();
const summary = extractOutputText(openaiJson);

if (!summary) {
  throw new Error("OpenAI response did not include output text.");
}

const content = truncateForDiscord(
  runUrl ? `${summary}\n\nGitHub Actions: ${runUrl}` : summary,
);

const discordResponse = await fetch(discordWebhookUrl, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    username: "Project Summary",
    content,
    allowed_mentions: { parse: [] },
  }),
});

if (!discordResponse.ok) {
  const errorBody = await discordResponse.text();
  throw new Error(`Discord webhook failed: ${discordResponse.status} ${errorBody}`);
}

console.log("Posted project summary to Discord.");
