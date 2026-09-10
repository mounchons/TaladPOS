import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
    // PrimeReact passthrough presets live here and are pure strings - without
    // this glob their classes are silently purged.
    "./src/styles/**/*.{js,ts}",
  ],
  theme: {
    extend: {
      colors: {
        // Cool galvanized-steel ground so produce colour reads against it,
        // the way fruit looks on market shelving.
        ink: {
          DEFAULT: "#17242F",
          700: "#24384A",
          500: "#4A6376",
          300: "#8598A6",
        },
        steel: {
          50: "#F0F3F5",
          100: "#E2E8EC",
          200: "#CFD8DE",
        },
        // Ripe Thai mango. Reserved for money and for "you are here".
        mango: {
          DEFAULT: "#F5A524",
          600: "#D98A0B",
          100: "#FDF0D6",
        },
        leaf: "#2F7D4F",
        chili: "#C8362B",
      },
      fontFamily: {
        display: ["var(--font-kanit)", "sans-serif"],
        sans: ["var(--font-plex-thai)", "sans-serif"],
      },
      borderRadius: {
        // Small and consistent on things you touch; structural panels stay square.
        control: "5px",
      },
    },
  },
  plugins: [],
};
export default config;
