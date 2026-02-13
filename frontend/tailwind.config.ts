import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        display: ["Space Grotesk", "sans-serif"],
        body: ["Instrument Sans", "sans-serif"],
      },
      colors: {
        bg: "hsl(var(--bg))",
        card: "hsl(var(--card))",
        ring: "hsl(var(--ring))",
        accent: "hsl(var(--accent))",
        primary: "hsl(var(--primary))",
        muted: "hsl(var(--muted))",
      },
    },
  },
  plugins: [],
} satisfies Config;
