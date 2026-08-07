namespace SsoAuthenticationServer.Models
{
	public class RegisterModel
	{
		public string Username { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
	}

	public class LoginModel
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}
	
	public class ValidateSSOTokenRequest
	{
		public string SSOToken { get; set; }
	}
}
