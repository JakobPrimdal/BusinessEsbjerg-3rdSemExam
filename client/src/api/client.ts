import { Api } from "./Api";

export const api = new Api({
  baseUrl: process.env.BUN_PUBLIC_API_URL ?? "http://localhost:5194",
  baseApiParams: { credentials: "include" },
});