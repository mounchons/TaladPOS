/** @type {import('postcss-load-config').Config} */
const config = {
  plugins: {
    // Tailwind 4 ships its PostCSS integration as a separate package; the bare
    // `tailwindcss` plugin entry is a v3 thing and throws on startup under v4.
    "@tailwindcss/postcss": {},
  },
};

export default config;
