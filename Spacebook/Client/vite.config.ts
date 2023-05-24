import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';
import fs from "fs";
import path from "path";

const entryDir = "./src/entry";

if (fs.existsSync(entryDir)) {
    fs.rmdirSync(entryDir, { recursive: true });
}
fs.mkdirSync(entryDir, { recursive: true });

fs.readdirSync("./src/components").forEach(filePath => {
    const componentName = path.basename(filePath, path.extname(filePath)).replace(".ce", "");

    const s = `import { defineCustomElement } from "vue";
import Component from "../components/${path.basename(filePath)}";
window.customElements.define('${componentName}', defineCustomElement(Component));`;

    fs.writeFileSync(`${entryDir}/${componentName}.js`, s);
});

fs.readdirSync("./src/pages").forEach(filePath => {
    const componentName = path.basename(filePath, path.extname(filePath));

    const s = `import { createApp, h } from "vue";
import wrapper from "vue3-webcomponent-wrapper";
import Page from "../pages/${path.basename(filePath)}";
window.customElements.define('${componentName}', wrapper(Page, createApp, h));`;

    fs.writeFileSync(`${entryDir}/${componentName}.js`, s);
});

let input = fs.readdirSync(entryDir).map(filePath => `${entryDir}/${filePath}`);
console.log(input);

export default defineConfig({
    plugins: [vue({
        template: {
            compilerOptions: {
                isCustomElement: (tag) => {
                    return tag.startsWith('file-uploader') || tag.startsWith('file-manager') || tag.startsWith('custom-image');
                }
            }
        }
    })],
    build: {
        rollupOptions: {
            treeshake: false,
            input: input,
            output: {
                dir: "../wwwroot/pages",
                entryFileNames: "[name].js",
                chunkFileNames: "[name].[hash].js"
            }
        }
    }
});