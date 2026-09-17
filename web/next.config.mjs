/** @type {import('next').NextConfig} */
const nextConfig = {
  // web/Dockerfile runs .next/standalone/server.js so the image carries only
  // the node_modules the server needs. `npm run dev` is unaffected.
  output: "standalone",
};

export default nextConfig;
