const fs = require("fs");
const path = require("path");

const source = path.resolve(__dirname, "../../Sovereign.UI/dist/sovereign-ui/browser");
const target = path.resolve(__dirname, "../../Sovereign.Extension/ui");

fs.rmSync(target, { recursive: true, force: true });
fs.mkdirSync(target, { recursive: true });
fs.cpSync(source, target, { recursive: true });

console.log("Copied UI build to extension:", target);