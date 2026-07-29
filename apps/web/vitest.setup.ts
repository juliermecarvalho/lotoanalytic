import "@testing-library/jest-dom/vitest";
import { afterAll, afterEach, beforeAll } from "vitest";
import { clearMockApiRequests } from "./tests/mocks/api";
import { server } from "./tests/mocks/server";

Object.defineProperty(window, "scrollTo", {
  value: () => undefined,
  writable: true
});

beforeAll(() => server.listen({ onUnhandledRequest: "error" }));
afterEach(() => {
  server.resetHandlers();
  clearMockApiRequests();
});
afterAll(() => server.close());
