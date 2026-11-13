import { Api } from "./Api";

const baseUrl = process.env.BUN_PUBLIC_API_URL;

if (!baseUrl) {
  throw new Error("BUN_PUBLIC_API_URL is not set");
}

export const api = new Api({
  baseUrl,
  baseApiParams: { credentials: "include" },
});