import { FetchArgs, createApi, fetchBaseQuery, retry } from '@reduxjs/toolkit/query/react'

const AUTH_FAILURE_DIAGNOSTIC_KEY = "refhub-auth-last-failure";

function getRequestUrl(args: string | FetchArgs): string {
  if (typeof args === "string") {
    return args;
  }

  return typeof args.url === "string" ? args.url : "unknown";
}

function getRequestMethod(args: string | FetchArgs): string {
  if (typeof args === "string") {
    return "GET";
  }

  return args.method ?? "GET";
}

function writeAuthDiagnostic(payload: Record<string, unknown>) {
  const diagnostic = {
    ...payload,
    route: window.location.pathname,
    search: window.location.search,
    at: new Date().toISOString(),
  };

  try {
    sessionStorage.setItem(AUTH_FAILURE_DIAGNOSTIC_KEY, JSON.stringify(diagnostic));
  } catch {
    // Session storage may be unavailable in some browser privacy modes.
  }

  console.warn("[AuthDiagnostic]", diagnostic);
}

/** if the query URL contains impersonate query we forward it with the API calls */
const fetchWithImpersonationQuery = (fetchFn: ReturnType<typeof fetchBaseQuery>) => async (args: string | FetchArgs, api, extraOptions) => {
  const impersonateKey = "impersonate";
  const impersonate = new URLSearchParams(location.search).get(impersonateKey);
  if (impersonate) {
    if (typeof args === "string") {
      const startingChar = args.includes('?') ? '&' : '?';
      args = `${args}${startingChar}${impersonateKey}=${impersonate}`;
    } else {
      args.params = args.params || {};
      args.params[impersonateKey] = impersonate;
    }
  }

  const result = await fetchFn(args, api, extraOptions);
  if ("error" in result) {
    const status = result.error?.status;
    if (status === 401 || status === 403) {
      writeAuthDiagnostic({
        endpoint: typeof api.endpoint === "string" ? api.endpoint : "unknown",
        method: getRequestMethod(args),
        url: getRequestUrl(args),
        status,
        source: "baseApi",
      });
    }
  }

  return result;
}

const fetchWithRetries = (fetchFn: ReturnType<typeof fetchBaseQuery>) => {
  return retry(fetchFn, {
    retryCondition: (error, _, extraArgs) => {
      // if we failed to execute the fetch call (e.g. due to network error) let's retry up to 3 times
      if (error.status === "FETCH_ERROR" && extraArgs.attempt <= 3) {
        return true;
      }
      return false;
    }
  })
}

// initialize an empty api service that we'll inject endpoints into later as needed
export const baseApi = createApi({
  baseQuery: fetchWithRetries(fetchWithImpersonationQuery(fetchBaseQuery({
    baseUrl: '/',
    prepareHeaders: (headers, api) => {
      if (api.endpoint == "setTestActive") {
        // without this, the boolean value is treated as text/plain and our backend complains
        headers.set("Content-Type", "application/json");
      }
    }
  }))),
  endpoints: () => ({}),
})