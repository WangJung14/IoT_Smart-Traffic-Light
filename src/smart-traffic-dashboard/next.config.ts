import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Disable StrictMode to prevent SignalR double-connect race condition.
  // React StrictMode (dev only) intentionally mounts→unmounts→remounts,
  // causing connection.stop() to be called while connection.start() is still
  // in flight — resulting in "Failed to start HttpConnection before stop()".
  reactStrictMode: false,
};

export default nextConfig;
