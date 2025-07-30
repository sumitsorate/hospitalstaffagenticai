/**
 * Represents the response returned after a successful login,
 * including authentication token and user details.
 */
export class LoginResponseDto {
  /**
   * JWT token issued to the authenticated user.
   */
  token: string = '';

  /**
   * Role name assigned to the user.
   */
  role: string = '';

  /**
   * Unique identifier for the staff member.
   */
  staffId: number = 0;

  /**
   * Full name of the authenticated user.
   */
  name: string = '';

  /**
   * Associated thread ID for AI agent conversation context.
   */
  threadId: string = '';
}
