import type { HttpRequest } from '@angular/common/http';

export function applyApiAuthHeader(
  request: HttpRequest<unknown>,
  token: string | null,
  apiUrl: string,
): HttpRequest<unknown> {
  if (!token || !request.url.startsWith(apiUrl) || request.headers.has('Authorization')) {
    return request;
  }

  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });
}
