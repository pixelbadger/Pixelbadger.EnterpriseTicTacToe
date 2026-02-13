import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const apiProxyTarget =
  process.env.services__api__https__0 ??
  process.env.services__api__http__0 ??
  process.env.VITE_API_PROXY_TARGET ??
  "http://localhost:5217";

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "../src/Pixelbadger.EnterpriseTicTacToe.Host/wwwroot",
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: apiProxyTarget,
        changeOrigin: true,
      },
      "/hubs": {
        target: apiProxyTarget,
        ws: true,
        changeOrigin: true,
      },
    },
  },
});
