import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns a failed HttpClient response into a single human-readable message, understanding both
 * error shapes the API can return: the { code, message } shape from ExceptionHandlingMiddleware
 * for business-rule/not-found errors, and ASP.NET Core's built-in ValidationProblemDetails shape
 * for model-validation (400) errors. Never surfaces raw JSON or stack traces to the user.
 */
export function extractErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Unable to reach the server. Please check your connection and try again.';
  }

  const body = error.error;

  if (body && typeof body === 'object') {
    if (typeof body.code === 'string' && typeof body.message === 'string') {
      return body.message;
    }

    if (body.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors as Record<string, string[]>).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (typeof body.title === 'string') {
      return body.title;
    }
  }

  return 'Something went wrong. Please try again.';
}
