using System;

namespace JuegoTCG.Networking
{
    [Serializable]
    public class GoogleSignInUser
    {
        public bool success;
        public string displayName;
        public string email;
        public string idToken;
        public string id;
        public string photoUrl;
        public string error;

        public string DisplayName => !string.IsNullOrEmpty(displayName) ? displayName : "Usuario Google";
        public string Email => email ?? "";
        public string IdToken => idToken ?? "";
        public string UserId => id ?? "";
        public string PhotoUrl => photoUrl ?? "";
    }
}
