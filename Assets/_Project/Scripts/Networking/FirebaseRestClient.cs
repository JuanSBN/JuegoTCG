using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using JuegoTCG.Social;

namespace JuegoTCG.Networking
{
    [Serializable]
    public class FirebaseAuthResult
    {
        public bool success;
        public string idToken;
        public string refreshToken;
        public string localId;
        public string displayName;
        public string photoUrl;
        public string email;
        public string error;
    }

    [Serializable]
    public class VitrineCloudData
    {
        public string userId;
        public string displayName;
        public string friendCode;
        public string avatarText;
        public string photoUrl;
        public List<string> cardIds = new List<string>();
        public int likesCount;
        public List<string> likedBy = new List<string>();
        public string updatedAt;

        public bool IsLikedBy(string uid) => likedBy != null && likedBy.Contains(uid);
    }

    public class FirestoreUserData
    {
        public string uid;
        public string displayName;
        public string friendCode;
        public string photoUrl;
        public int level = 1;
        public int collectionPower = 0;
        public int albumProgress = 0;
        public int coins = 0;
        public Dictionary<string, int> ownedCards = new Dictionary<string, int>();
        public List<FriendData> friends = new List<FriendData>();
        public List<string> ideal11 = new List<string>();
        public List<string> featuredCards = new List<string>();
    }

    public class FriendStatsUpdate
    {
        public string uid;
        public string displayName;
        public string friendCode;
        public int level;
        public int collectionPower;
        public int albumProgress;
    }

    /// <summary>
    /// Cliente REST nativo para Firebase Auth y Cloud Firestore usando UnityWebRequest.
    /// No requiere SDKs externos ni plugins nativos, garantizando compatibilidad 100% en Android.
    /// </summary>
    public static class FirebaseRestClient
    {
        public const string ApiKey = "AIzaSyBTk82R_Pvyf4NWtzN2yVcxadCE4bOCefg";
        public const string ProjectId = "juegotcg-dev";

        private const string AuthBaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts";
        private const string SecureTokenUrl = "https://securetoken.googleapis.com/v1/token";
        private const string FirestoreBaseUrl = "https://firestore.googleapis.com/v1/projects/" + ProjectId + "/databases/(default)/documents";

        // =========================================================================
        // 1. AUTENTICACI�N FIREBASE AUTH
        // =========================================================================

        /// <summary>
        /// Inicia sesi�n an�nima en Firebase Auth y obtiene tokens JWT.
        /// </summary>
        public static async Task<FirebaseAuthResult> SignInAnonymouslyAsync()
        {
            string url = $"{AuthBaseUrl}:signUp?key={ApiKey}";
            string body = "{\"returnSecureToken\":true}";

            string responseJson = await PostJsonAsync(url, body, null);
            if (string.IsNullOrEmpty(responseJson))
            {
                return new FirebaseAuthResult { success = false, error = "Error de conexi�n con Firebase Auth." };
            }

            if (responseJson.Contains("\"error\""))
            {
                return new FirebaseAuthResult { success = false, error = ExtractJsonString(responseJson, "message") };
            }

            return new FirebaseAuthResult
            {
                success = true,
                idToken = ExtractJsonString(responseJson, "idToken"),
                refreshToken = ExtractJsonString(responseJson, "refreshToken"),
                localId = ExtractJsonString(responseJson, "localId")
            };
        }

        /// <summary>
        /// Inicia sesión con Email y Contraseña en Firebase Auth.
        /// </summary>
        public static async Task<FirebaseAuthResult> SignInWithEmailPasswordAsync(string email, string password)
        {
            string url = $"{AuthBaseUrl}:signInWithPassword?key={ApiKey}";
            string body = "{\"email\":\"" + Escape(email) + "\",\"password\":\"" + Escape(password) + "\",\"returnSecureToken\":true}";

            string responseJson = await PostJsonAsync(url, body, null);
            if (string.IsNullOrEmpty(responseJson))
            {
                return new FirebaseAuthResult { success = false, error = "Error de conexión con Firebase Auth." };
            }

            if (responseJson.Contains("\"error\""))
            {
                return new FirebaseAuthResult { success = false, error = ExtractJsonString(responseJson, "message") };
            }

            return new FirebaseAuthResult
            {
                success = true,
                idToken = ExtractJsonString(responseJson, "idToken"),
                refreshToken = ExtractJsonString(responseJson, "refreshToken"),
                localId = ExtractJsonString(responseJson, "localId"),
                displayName = ExtractJsonString(responseJson, "displayName"),
                email = ExtractJsonString(responseJson, "email")
            };
        }

        /// <summary>
        /// Registra un nuevo usuario con Email y Contraseña en Firebase Auth.
        /// </summary>
        public static async Task<FirebaseAuthResult> SignUpWithEmailPasswordAsync(string email, string password)
        {
            string url = $"{AuthBaseUrl}:signUp?key={ApiKey}";
            string body = "{\"email\":\"" + Escape(email) + "\",\"password\":\"" + Escape(password) + "\",\"returnSecureToken\":true}";

            string responseJson = await PostJsonAsync(url, body, null);
            if (string.IsNullOrEmpty(responseJson))
            {
                return new FirebaseAuthResult { success = false, error = "Error de conexión con Firebase Auth." };
            }

            if (responseJson.Contains("\"error\""))
            {
                return new FirebaseAuthResult { success = false, error = ExtractJsonString(responseJson, "message") };
            }

            return new FirebaseAuthResult
            {
                success = true,
                idToken = ExtractJsonString(responseJson, "idToken"),
                refreshToken = ExtractJsonString(responseJson, "refreshToken"),
                localId = ExtractJsonString(responseJson, "localId"),
                displayName = ExtractJsonString(responseJson, "displayName"),
                email = ExtractJsonString(responseJson, "email")
            };
        }

        /// <summary>
        /// Intercambia un Google ID Token nativo por credenciales de Firebase Auth.
        /// </summary>
        public static async Task<FirebaseAuthResult> SignInWithGoogleIdTokenAsync(string googleIdToken)
        {
            if (string.IsNullOrEmpty(googleIdToken))
            {
                return await SignInAnonymouslyAsync();
            }

            string url = $"{AuthBaseUrl}:signInWithIdp?key={ApiKey}";
            string postBody = $"id_token={googleIdToken}&providerId=google.com";
            string body = "{\"postBody\":\"" + postBody + "\",\"requestUri\":\"http://localhost\",\"returnSecureToken\":true,\"returnIdpCredential\":true}";

            string responseJson = await PostJsonAsync(url, body, null);
            if (string.IsNullOrEmpty(responseJson) || responseJson.Contains("\"error\""))
            {
                Debug.LogWarning("[FirebaseRest] Fall� intercambio de token Google, usando an�nimo como respaldo.");
                return await SignInAnonymouslyAsync();
            }

            return new FirebaseAuthResult
            {
                success = true,
                idToken = ExtractJsonString(responseJson, "idToken"),
                refreshToken = ExtractJsonString(responseJson, "refreshToken"),
                localId = ExtractJsonString(responseJson, "localId"),
                displayName = ExtractJsonString(responseJson, "displayName"),
                photoUrl = ExtractJsonString(responseJson, "photoUrl"),
                email = ExtractJsonString(responseJson, "email")
            };
        }

        /// <summary>
        /// Renueva el token de acceso idToken usando el refreshToken.
        /// </summary>
        public static async Task<string> RefreshIdTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) return null;

            string url = $"{SecureTokenUrl}?key={ApiKey}";
            string body = "{\"grant_type\":\"refresh_token\",\"refresh_token\":\"" + refreshToken + "\"}";

            string res = await PostJsonAsync(url, body, null);
            if (!string.IsNullOrEmpty(res) && !res.Contains("\"error\""))
            {
                return ExtractJsonString(res, "id_token");
            }
            return null;
        }

        // =========================================================================
        // 2. PERFILES DE USUARIO EN FIRESTORE (/users/{uid})
        // =========================================================================

        /// <summary>
        /// Registra o actualiza el documento del usuario en /users/{uid}.
        /// </summary>
        /// <summary>
        /// Registra o actualiza el documento del usuario en /users/{uid}, incluyendo su inventario de cartas.
        /// </summary>
        public static async Task<bool> UpsertUserDocAsync(
            string idToken, 
            string uid, 
            string displayName, 
            string friendCode, 
            string photoUrl, 
            int level, 
            int power, 
            int albumProgress, 
            int coins = 0,
            List<FriendData> friends = null,
            Dictionary<string, int> ownedCards = null,
            List<string> ideal11 = null,
            List<string> featuredCards = null)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(uid)) return false;

            string url = $"{FirestoreBaseUrl}/users/{uid}";

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"fields\":{");
            sb.Append($"\"uid\":{{\"stringValue\":\"{Escape(uid)}\"}},");
            sb.Append($"\"displayName\":{{\"stringValue\":\"{Escape(displayName)}\"}},");
            sb.Append($"\"friendCode\":{{\"stringValue\":\"{Escape(friendCode)}\"}},");
            sb.Append($"\"photoUrl\":{{\"stringValue\":\"{Escape(photoUrl ?? "")}\"}},");
            sb.Append($"\"level\":{{\"integerValue\":{level}}},");
            sb.Append($"\"collectionPower\":{{\"integerValue\":{power}}},");
            sb.Append($"\"albumProgress\":{{\"integerValue\":{albumProgress}}},");
            sb.Append($"\"coins\":{{\"integerValue\":{coins}}},");
            sb.Append($"\"createdAt\":{{\"stringValue\":\"{DateTime.UtcNow:o}\"}}");

            if (friends != null)
            {
                if (friends.Count > 0)
                {
                    sb.Append(",\"friends\":{\"arrayValue\":{\"values\":[");
                    for (int i = 0; i < friends.Count; i++)
                    {
                        var f = friends[i];
                        if (i > 0) sb.Append(",");
                        sb.Append("{\"mapValue\":{\"fields\":{");
                        sb.Append($"\"friendUid\":{{\"stringValue\":\"{Escape(f.friendUid)}\"}},");
                        sb.Append($"\"displayName\":{{\"stringValue\":\"{Escape(f.displayName)}\"}},");
                        sb.Append($"\"friendCode\":{{\"stringValue\":\"{Escape(f.friendCode)}\"}},");
                        sb.Append($"\"level\":{{\"integerValue\":{f.level}}},");
                        sb.Append($"\"collectionPower\":{{\"integerValue\":{f.collectionPower}}},");
                        sb.Append($"\"albumProgress\":{{\"integerValue\":{f.albumProgress}}}");
                        sb.Append("}}}");
                    }
                    sb.Append("]}}");
                }
                else
                {
                    sb.Append(",\"friends\":{\"arrayValue\":{}}");
                }
            }

            if (ownedCards != null && ownedCards.Count > 0)
            {
                sb.Append(",\"ownedCards\":{\"mapValue\":{\"fields\":{");
                int idx = 0;
                foreach (var kvp in ownedCards)
                {
                    if (idx > 0) sb.Append(",");
                    sb.Append($"\"{Escape(kvp.Key)}\":{{\"integerValue\":{kvp.Value}}}");
                    idx++;
                }
                sb.Append("}}}");
            }
            else if (ownedCards != null)
            {
                sb.Append(",\"ownedCards\":{\"mapValue\":{}}");
            }

            if (ideal11 != null)
            {
                if (ideal11.Count > 0)
                {
                    sb.Append(",\"ideal11\":{\"arrayValue\":{\"values\":[");
                    for (int i = 0; i < ideal11.Count; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append($"{{\"stringValue\":\"{Escape(ideal11[i] ?? "")}\"}}");
                    }
                    sb.Append("]}}");
                }
                else
                {
                    sb.Append(",\"ideal11\":{\"arrayValue\":{}}");
                }
            }

            if (featuredCards != null)
            {
                if (featuredCards.Count > 0)
                {
                    sb.Append(",\"featuredCards\":{\"arrayValue\":{\"values\":[");
                    for (int i = 0; i < featuredCards.Count; i++)
                    {
                        if (i > 0) sb.Append(",");
                        sb.Append($"{{\"stringValue\":\"{Escape(featuredCards[i] ?? "")}\"}}");
                    }
                    sb.Append("]}}");
                }
                else
                {
                    sb.Append(",\"featuredCards\":{\"arrayValue\":{}}");
                }
            }

            sb.Append("}}");

            string res = await PatchJsonAsync(url, sb.ToString(), idToken);
            bool ok = !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
            if (ok) Debug.Log($"<color=green>[FirebaseRest] Perfil e inventario sincronizados en Firestore: {displayName} ({friendCode})</color>");
            else Debug.LogWarning($"[FirebaseRest] Error sincronizando perfil: {res}");
            return ok;
        }

        /// <summary>
        /// Obtiene el documento completo de un usuario en Firestore (/users/{targetUid}).
        /// Permite leer su inventario real de cartas para la comparación lado a lado de álbumes.
        /// </summary>
        public static async Task<FirestoreUserData> GetUserDocAsync(string idToken, string targetUid)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(targetUid)) return null;

            string url = $"{FirestoreBaseUrl}/users/{targetUid}";
            string res = await GetJsonAsync(url, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return null;

            return new FirestoreUserData
            {
                uid = ExtractFieldString(res, "uid"),
                displayName = ExtractFieldString(res, "displayName"),
                friendCode = ExtractFieldString(res, "friendCode"),
                photoUrl = ExtractFieldString(res, "photoUrl"),
                level = Mathf.Max(1, ExtractFieldInt(res, "level")),
                collectionPower = ExtractFieldInt(res, "collectionPower"),
                albumProgress = ExtractFieldInt(res, "albumProgress"),
                coins = ExtractFieldInt(res, "coins"),
                ownedCards = ParseOwnedCardsMap(res),
                friends = ParseFriendsArray(res),
                ideal11 = ParseStringArray(res, "ideal11"),
                featuredCards = ParseStringArray(res, "featuredCards")
            };
        }

        /// <summary>
        /// Busca un jugador en Firestore por su c�digo de amigo exacto (ej: FC-1234).
        /// </summary>
        public static async Task<FirestoreUserData> GetUserByFriendCodeAsync(string idToken, string friendCode)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(friendCode)) return null;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"users\"}],\"where\":{\"fieldFilter\":{\"field\":{\"fieldPath\":\"friendCode\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(friendCode.Trim().ToUpper()) + "\"}}},\"limit\":1}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return null;

            return ParseUserDocFromQuery(res);
        }

        // =========================================================================
        // 3. SOLICITUDES DE AMISTAD EN FIRESTORE (/friendRequests/{id})
        // =========================================================================

        /// <summary>
        /// Env�a una solicitud de amistad creando un documento en /friendRequests/{requestId}.
        /// </summary>
        public static async Task<bool> SendFriendRequestAsync(string idToken, string fromUid, string fromName, string fromCode, string toUid, string toName, string toCode)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(fromUid) || string.IsNullOrEmpty(toUid)) return false;

            string requestId = $"req_{fromUid}_{toUid}";
            string url = $"{FirestoreBaseUrl}/friendRequests/{requestId}";

            string body = "{\"fields\":{" +
                $"\"requestId\":{{\"stringValue\":\"{Escape(requestId)}\"}}," +
                $"\"fromUid\":{{\"stringValue\":\"{Escape(fromUid)}\"}}," +
                $"\"fromName\":{{\"stringValue\":\"{Escape(fromName)}\"}}," +
                $"\"fromCode\":{{\"stringValue\":\"{Escape(fromCode)}\"}}," +
                $"\"toUid\":{{\"stringValue\":\"{Escape(toUid)}\"}}," +
                $"\"toName\":{{\"stringValue\":\"{Escape(toName)}\"}}," +
                $"\"toCode\":{{\"stringValue\":\"{Escape(toCode)}\"}}," +
                $"\"status\":{{\"stringValue\":\"pending\"}}," +
                $"\"createdAt\":{{\"stringValue\":\"{DateTime.UtcNow:o}\"}}" +
                "}}";

            string res = await PatchJsonAsync(url, body, idToken);
            bool ok = !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
            if (ok) Debug.Log($"<color=green>[FirebaseRest] Solicitud enviada en la nube de {fromName} para {toName}</color>");
            else Debug.LogWarning($"[FirebaseRest] Error enviando solicitud: {res}");
            return ok;
        }

        /// <summary>
        /// Obtiene todas las solicitudes entrantes dirigidas al usuario actual con status="pending".
        /// </summary>
        public static async Task<List<FriendRequestData>> GetIncomingRequestsAsync(string idToken, string myUid)
        {
            var list = new List<FriendRequestData>();
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(myUid)) return list;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"friendRequests\"}],\"where\":{\"compositeFilter\":{\"op\":\"AND\",\"filters\":[" +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"toUid\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(myUid) + "\"}}}," +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"status\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"pending\"}}}" +
                "]}}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return list;

            return ParseFriendRequests(res);
        }

        /// <summary>
        /// Consulta si alguna solicitud enviada por el usuario fue aceptada para agregarlo a amigos.
        /// Ultra-rápido: no realiza llamadas anidadas individuales a Firestore; los stats se actualizan en lote.
        /// </summary>
        public static async Task<List<FriendData>> GetAcceptedOutgoingRequestsAsync(string idToken, string myUid)
        {
            var newFriends = new List<FriendData>();
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(myUid)) return newFriends;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"friendRequests\"}],\"where\":{\"compositeFilter\":{\"op\":\"AND\",\"filters\":[" +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"fromUid\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(myUid) + "\"}}}," +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"status\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"accepted\"}}}" +
                "]}}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return newFriends;

            var reqs = ParseFriendRequests(res);
            foreach (var r in reqs)
            {
                // El amigo nuevo para el remitente (myUid) es el destinatario (toUid, toName, toCode)
                string targetUid = r.toUid;
                string targetName = r.toName;
                string targetCode = r.toCode;

                if (string.IsNullOrEmpty(targetUid) || targetUid == myUid) continue;

                newFriends.Add(new FriendData
                {
                    friendUid = targetUid,
                    displayName = !string.IsNullOrEmpty(targetName) ? targetName : "Entrenador",
                    friendCode = targetCode,
                    level = 1,
                    collectionPower = 0,
                    albumProgress = 0
                });
            }
            return newFriends;
        }

        /// <summary>
        /// Consulta si alguna solicitud entrante (dirigida al usuario actual) fue aceptada para agregarlo a amigos (persistencia bidireccional).
        /// Ultra-rápido: no realiza llamadas anidadas individuales a Firestore; los stats se actualizan en lote.
        /// </summary>
        public static async Task<List<FriendData>> GetAcceptedIncomingRequestsAsync(string idToken, string myUid)
        {
            var newFriends = new List<FriendData>();
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(myUid)) return newFriends;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"friendRequests\"}],\"where\":{\"compositeFilter\":{\"op\":\"AND\",\"filters\":[" +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"toUid\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(myUid) + "\"}}}," +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"status\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"accepted\"}}}" +
                "]}}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return newFriends;

            var reqs = ParseFriendRequests(res);
            foreach (var r in reqs)
            {
                // Para el destinatario (myUid), el amigo nuevo es el remitente (fromUid, fromName, fromCode)
                string targetUid = r.fromUid;
                string targetName = r.fromName;
                string targetCode = r.fromCode;

                if (string.IsNullOrEmpty(targetUid) || targetUid == myUid) continue;

                newFriends.Add(new FriendData
                {
                    friendUid = targetUid,
                    displayName = !string.IsNullOrEmpty(targetName) ? targetName : "Entrenador",
                    friendCode = targetCode,
                    level = 1,
                    collectionPower = 0,
                    albumProgress = 0
                });
            }
            return newFriends;
        }

        /// <summary>
        /// Consulta en un SOLO llamado HTTP ultra-liviano los datos esenciales de una lista de amigos.
        /// Utiliza el endpoint :batchGet de Firestore con máscara de campos para reducir la latencia a menos de 200ms
        /// y evitar la descarga redundante de inventarios enteros de cartas.
        /// </summary>
        public static async Task<Dictionary<string, FriendStatsUpdate>> BatchGetUsersStatsAsync(string idToken, List<string> uids)
        {
            var result = new Dictionary<string, FriendStatsUpdate>();
            if (string.IsNullOrEmpty(idToken) || uids == null || uids.Count == 0) return result;

            var distinctUids = uids.Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
            if (distinctUids.Count == 0) return result;

            string batchUrl = $"{FirestoreBaseUrl}:batchGet";

            var sb = new StringBuilder();
            sb.Append("{\"documents\":[");
            for (int i = 0; i < distinctUids.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append($"\"projects/{ProjectId}/databases/(default)/documents/users/{Escape(distinctUids[i])}\"");
            }
            sb.Append("],\"mask\":{\"fieldPaths\":[\"displayName\",\"friendCode\",\"level\",\"collectionPower\",\"albumProgress\"]}}");

            string res = await PostJsonAsync(batchUrl, sb.ToString(), idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return result;

            var docMatches = Regex.Matches(res, "\"found\"\\s*:\\s*\\{([\\s\\S]*?)\"readTime\"");
            foreach (Match m in docMatches)
            {
                string block = m.Value;
                var uidMatch = Regex.Match(block, "\"name\"\\s*:\\s*\"[^\"]*/users/([^\"]+)\"");
                if (!uidMatch.Success) continue;

                string uid = uidMatch.Groups[1].Value;
                string displayName = ExtractFieldString(block, "displayName");
                string friendCode = ExtractFieldString(block, "friendCode");
                int level = Mathf.Max(1, ExtractFieldInt(block, "level"));
                int power = ExtractFieldInt(block, "collectionPower");
                int progress = ExtractFieldInt(block, "albumProgress");

                result[uid] = new FriendStatsUpdate
                {
                    uid = uid,
                    displayName = displayName,
                    friendCode = friendCode,
                    level = level,
                    collectionPower = power,
                    albumProgress = progress
                };
            }

            return result;
        }

        /// <summary>
        /// Actualiza el estado de una solicitud de amistad ("accepted" o "rejected").
        /// </summary>
        public static async Task<bool> UpdateFriendRequestStatusAsync(string idToken, string requestId, string newStatus)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(requestId)) return false;

            string url = $"{FirestoreBaseUrl}/friendRequests/{requestId}?updateMask.fieldPaths=status";
            string body = "{\"fields\":{\"status\":{\"stringValue\":\"" + newStatus + "\"}}}";

            string res = await PatchJsonAsync(url, body, idToken);
            return !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
        }

                // =========================================================================
        // 4. OFERTAS DE INTERCAMBIO (/tradeOffers/{id})
        // =========================================================================

        public static async Task<bool> CreateTradeOfferAsync(string idToken, TradeOfferItem offer)
        {
            if (string.IsNullOrEmpty(idToken) || offer == null || string.IsNullOrEmpty(offer.tradeId)) return false;

            string url = $"{FirestoreBaseUrl}/tradeOffers/{offer.tradeId}";
            string body = "{\"fields\":{" +
                $"\"tradeId\":{{\"stringValue\":\"{Escape(offer.tradeId)}\"}}," +
                $"\"fromUid\":{{\"stringValue\":\"{Escape(offer.fromUid)}\"}}," +
                $"\"fromDisplayName\":{{\"stringValue\":\"{Escape(offer.fromDisplayName)}\"}}," +
                $"\"toUid\":{{\"stringValue\":\"{Escape(offer.toUid)}\"}}," +
                $"\"toDisplayName\":{{\"stringValue\":\"{Escape(offer.toDisplayName)}\"}}," +
                $"\"offeredCardId\":{{\"stringValue\":\"{Escape(offer.offeredCardId)}\"}}," +
                $"\"offeredCardName\":{{\"stringValue\":\"{Escape(offer.offeredCardName)}\"}}," +
                $"\"offeredRarity\":{{\"stringValue\":\"{Escape(offer.offeredRarity)}\"}}," +
                $"\"requestedCardId\":{{\"stringValue\":\"{Escape(offer.requestedCardId)}\"}}," +
                $"\"requestedCardName\":{{\"stringValue\":\"{Escape(offer.requestedCardName)}\"}}," +
                $"\"requestedRarity\":{{\"stringValue\":\"{Escape(offer.requestedRarity)}\"}}," +
                $"\"status\":{{\"stringValue\":\"{Escape(offer.status)}\"}}," +
                $"\"createdAt\":{{\"stringValue\":\"{DateTime.UtcNow:o}\"}}" +
                "}}";

            string res = await PatchJsonAsync(url, body, idToken);
            bool ok = !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
            if (ok) Debug.Log($"<color=green>[FirebaseRest] Oferta de intercambio {offer.tradeId} creada en Firestore.</color>");
            else Debug.LogWarning($"[FirebaseRest] Error creando oferta de intercambio: {res}");
            return ok;
        }

        public static async Task<List<TradeOfferItem>> GetIncomingTradeOffersAsync(string idToken, string myUid)
        {
            var list = new List<TradeOfferItem>();
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(myUid)) return list;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"tradeOffers\"}],\"where\":{\"compositeFilter\":{\"op\":\"AND\",\"filters\":[" +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"toUid\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(myUid) + "\"}}}," +
                "{\"fieldFilter\":{\"field\":{\"fieldPath\":\"status\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"pendiente\"}}}" +
                "]}}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res)) return list;
            if (res.Contains("\"error\""))
            {
                Debug.LogWarning($"[FirebaseRest] Error en GetIncomingTradeOffersAsync (myUid={myUid}): {res}");
                return list;
            }

            return ParseTradeOffers(res, isIncoming: true);
        }

        public static async Task<List<TradeOfferItem>> GetIncomingTradeOffersAllStatusAsync(string idToken, string myUid)
        {
            var list = new List<TradeOfferItem>();
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(myUid)) return list;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"tradeOffers\"}],\"where\":{" +
                "\"fieldFilter\":{\"field\":{\"fieldPath\":\"toUid\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(myUid) + "\"}}" +
                "}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return list;

            return ParseTradeOffers(res, isIncoming: true);
        }

        public static async Task<List<TradeOfferItem>> GetSentTradeOffersAsync(string idToken, string myUid)
        {
            var list = new List<TradeOfferItem>();
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(myUid)) return list;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"tradeOffers\"}],\"where\":{" +
                "\"fieldFilter\":{\"field\":{\"fieldPath\":\"fromUid\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(myUid) + "\"}}" +
                "}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res)) return list;
            if (res.Contains("\"error\""))
            {
                Debug.LogWarning($"[FirebaseRest] Error en GetSentTradeOffersAsync (myUid={myUid}): {res}");
                return list;
            }

            return ParseTradeOffers(res, isIncoming: false);
        }

        public static async Task<bool> UpdateTradeOfferStatusAsync(string idToken, string tradeId, string newStatus)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(tradeId)) return false;

            string url = $"{FirestoreBaseUrl}/tradeOffers/{tradeId}?updateMask.fieldPaths=status";
            string body = "{\"fields\":{\"status\":{\"stringValue\":\"" + Escape(newStatus) + "\"}}}";

            string res = await PatchJsonAsync(url, body, idToken);
            return !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
        }

        // =========================================================================
        // 5. MERCADO DE CARTAS (/marketListings/{listingId})
        // =========================================================================

        public static async Task<bool> CreateMarketListingAsync(string idToken, MarketListingData listing)
        {
            if (string.IsNullOrEmpty(idToken) || listing == null) return false;

            string url = $"{FirestoreBaseUrl}/marketListings/{listing.listingId}";

            var sb = new StringBuilder();
            sb.Append("{\"fields\":{");
            sb.Append($"\"listingId\":{{\"stringValue\":\"{Escape(listing.listingId)}\"}},");
            sb.Append($"\"sellerUid\":{{\"stringValue\":\"{Escape(listing.sellerUid)}\"}},");
            sb.Append($"\"sellerDisplayName\":{{\"stringValue\":\"{Escape(listing.sellerDisplayName)}\"}},");
            sb.Append($"\"cardId\":{{\"stringValue\":\"{Escape(listing.cardId)}\"}},");
            sb.Append($"\"cardName\":{{\"stringValue\":\"{Escape(listing.cardName)}\"}},");
            sb.Append($"\"initials\":{{\"stringValue\":\"{Escape(listing.initials)}\"}},");
            sb.Append($"\"rarity\":{{\"stringValue\":\"{Escape(listing.rarity)}\"}},");
            sb.Append($"\"quantity\":{{\"integerValue\":{listing.quantity}}},");
            sb.Append($"\"pricePerCard\":{{\"integerValue\":{listing.pricePerCard}}},");
            sb.Append($"\"status\":{{\"stringValue\":\"activo\"}},");
            sb.Append($"\"createdAt\":{{\"stringValue\":\"{DateTime.UtcNow:o}\"}}");
            sb.Append("}}");

            string res = await PatchJsonAsync(url, sb.ToString(), idToken);
            bool ok = !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
            if (ok) Debug.Log($"<color=green>[FirebaseRest] Publicación {listing.listingId} creada en Firestore ({listing.cardName} por {listing.pricePerCard} monedas).</color>");
            else Debug.LogWarning($"[FirebaseRest] Error creando publicación: {res}");
            return ok;
        }

        public static async Task<List<MarketListingData>> GetActiveMarketListingsAsync(string idToken, string myUid)
        {
            var list = new List<MarketListingData>();
            if (string.IsNullOrEmpty(idToken)) return list;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"marketListings\"}],\"where\":{" +
                "\"fieldFilter\":{\"field\":{\"fieldPath\":\"status\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"activo\"}}" +
                "}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return list;

            return ParseMarketListings(res, myUid);
        }

        public static async Task<List<MarketListingData>> GetMyMarketListingsAsync(string idToken, string myUid)
        {
            var list = new List<MarketListingData>();
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(myUid)) return list;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"marketListings\"}],\"where\":{" +
                "\"fieldFilter\":{\"field\":{\"fieldPath\":\"sellerUid\"},\"op\":\"EQUAL\",\"value\":{\"stringValue\":\"" + Escape(myUid) + "\"}}" +
                "}}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return list;

            return ParseMarketListings(res, myUid);
        }

        public static async Task<bool> BuyMarketListingAsync(string idToken, string listingId, string buyerUid, string buyerDisplayName)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(listingId)) return false;

            string url = $"{FirestoreBaseUrl}/marketListings/{listingId}?updateMask.fieldPaths=status&updateMask.fieldPaths=buyerUid&updateMask.fieldPaths=buyerDisplayName";
            string body = "{\"fields\":{" +
                "\"status\":{\"stringValue\":\"vendido\"}," +
                $"\"buyerUid\":{{\"stringValue\":\"{Escape(buyerUid)}\"}}," +
                $"\"buyerDisplayName\":{{\"stringValue\":\"{Escape(buyerDisplayName)}\"}}" +
                "}}";

            string res = await PatchJsonAsync(url, body, idToken);
            return !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
        }

        public static async Task<bool> UpdateMarketListingStatusAsync(string idToken, string listingId, string newStatus)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(listingId)) return false;

            string url = $"{FirestoreBaseUrl}/marketListings/{listingId}?updateMask.fieldPaths=status";
            string body = "{\"fields\":{\"status\":{\"stringValue\":\"" + Escape(newStatus) + "\"}}}";

            string res = await PatchJsonAsync(url, body, idToken);
            return !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
        }

        public static async Task<bool> CancelMarketListingAsync(string idToken, string listingId)
        {
            return await UpdateMarketListingStatusAsync(idToken, listingId, "cancelado");
        }

        public static async Task<bool> UpdateMarketListingPriceAsync(string idToken, string listingId, int newPrice)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(listingId)) return false;

            string url = $"{FirestoreBaseUrl}/marketListings/{listingId}?updateMask.fieldPaths=pricePerCard";
            string body = $"{{\"fields\":{{\"pricePerCard\":{{\"integerValue\":{newPrice}}}}}}}";

            string res = await PatchJsonAsync(url, body, idToken);
            return !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
        }

// =========================================================================
        // 6. VITRINAS PÚBLICAS Y SHOWCASE (/vitrines/{userId})
        // =========================================================================

        public static async Task<bool> UpsertVitrineDocAsync(
            string idToken,
            string userId,
            string displayName,
            string friendCode,
            string avatarText,
            string photoUrl,
            List<string> cardIds,
            int likesCount = 0,
            List<string> likedBy = null)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(userId)) return false;

            string url = $"{FirestoreBaseUrl}/vitrines/{userId}";

            var sb = new StringBuilder();
            sb.Append("{\"fields\":{");
            sb.Append($"\"userId\":{{\"stringValue\":\"{Escape(userId)}\"}},");
            sb.Append($"\"displayName\":{{\"stringValue\":\"{Escape(displayName)}\"}},");
            sb.Append($"\"friendCode\":{{\"stringValue\":\"{Escape(friendCode)}\"}},");
            sb.Append($"\"avatarText\":{{\"stringValue\":\"{Escape(avatarText)}\"}},");
            sb.Append($"\"photoUrl\":{{\"stringValue\":\"{Escape(photoUrl ?? "")}\"}},");
            sb.Append($"\"likesCount\":{{\"integerValue\":{likesCount}}},");
            sb.Append($"\"updatedAt\":{{\"stringValue\":\"{DateTime.UtcNow:o}\"}}");

            if (cardIds != null && cardIds.Count > 0)
            {
                sb.Append(",\"cardIds\":{\"arrayValue\":{\"values\":[");
                for (int i = 0; i < cardIds.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"{{\"stringValue\":\"{Escape(cardIds[i])}\"}}");
                }
                sb.Append("]}}");
            }
            else
            {
                sb.Append(",\"cardIds\":{\"arrayValue\":{}}");
            }

            if (likedBy != null && likedBy.Count > 0)
            {
                sb.Append(",\"likedBy\":{\"arrayValue\":{\"values\":[");
                for (int i = 0; i < likedBy.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"{{\"stringValue\":\"{Escape(likedBy[i])}\"}}");
                }
                sb.Append("]}}");
            }
            else
            {
                sb.Append(",\"likedBy\":{\"arrayValue\":{}}");
            }

            sb.Append("}}");

            string res = await PatchJsonAsync(url, sb.ToString(), idToken);
            bool ok = !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
            if (ok) Debug.Log($"<color=green>[FirebaseRest] Vitrina pública guardada para {displayName} ({cardIds?.Count ?? 0} cartas)</color>");
            else Debug.LogWarning($"[FirebaseRest] Error guardando vitrina: {res}");
            return ok;
        }

        public static async Task<VitrineCloudData> GetVitrineDocAsync(string idToken, string targetUserId)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(targetUserId)) return null;

            string url = $"{FirestoreBaseUrl}/vitrines/{targetUserId}";
            string res = await GetJsonAsync(url, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return null;

            return ParseVitrineDoc(res);
        }

        public static async Task<List<VitrineCloudData>> GetPublicVitrinesAsync(string idToken, int limit = 20)
        {
            var list = new List<VitrineCloudData>();
            if (string.IsNullOrEmpty(idToken)) return list;

            string url = $"{FirestoreBaseUrl}:runQuery";
            string queryJson = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"vitrines\"}],\"orderBy\":[{\"field\":{\"fieldPath\":\"likesCount\"},\"direction\":\"DESCENDING\"}],\"limit\":" + limit + "}}";

            string res = await PostJsonAsync(url, queryJson, idToken);
            if (string.IsNullOrEmpty(res) || res.Contains("\"error\"")) return list;

            return ParseVitrinesListFromQuery(res);
        }

        public static async Task<bool> ToggleVitrineLikeAsync(
            string idToken,
            string vitrineUserId,
            string myUid,
            bool isLiked,
            int currentLikes,
            List<string> currentLikedBy)
        {
            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(vitrineUserId) || string.IsNullOrEmpty(myUid)) return false;

            int newLikes = isLiked ? Mathf.Max(0, currentLikes - 1) : currentLikes + 1;
            var newLikedBy = new List<string>(currentLikedBy ?? new List<string>());

            if (isLiked)
            {
                newLikedBy.Remove(myUid);
            }
            else
            {
                if (!newLikedBy.Contains(myUid)) newLikedBy.Add(myUid);
            }

            string url = $"{FirestoreBaseUrl}/vitrines/{vitrineUserId}?updateMask.fieldPaths=likesCount&updateMask.fieldPaths=likedBy";

            var sb = new StringBuilder();
            sb.Append("{\"fields\":{");
            sb.Append($"\"likesCount\":{{\"integerValue\":{newLikes}}}");

            if (newLikedBy.Count > 0)
            {
                sb.Append(",\"likedBy\":{\"arrayValue\":{\"values\":[");
                for (int i = 0; i < newLikedBy.Count; i++)
                {
                    if (i > 0) sb.Append(",");
                    sb.Append($"{{\"stringValue\":\"{Escape(newLikedBy[i])}\"}}");
                }
                sb.Append("]}}");
            }
            else
            {
                sb.Append(",\"likedBy\":{\"arrayValue\":{}}");
            }

            sb.Append("}}");

            string res = await PatchJsonAsync(url, sb.ToString(), idToken);
            return !string.IsNullOrEmpty(res) && !res.Contains("\"error\"");
        }

        private static VitrineCloudData ParseVitrineDoc(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Contains("\"error\"")) return null;

            return new VitrineCloudData
            {
                userId = ExtractFieldString(json, "userId"),
                displayName = ExtractFieldString(json, "displayName"),
                friendCode = ExtractFieldString(json, "friendCode"),
                avatarText = ExtractFieldString(json, "avatarText"),
                photoUrl = ExtractFieldString(json, "photoUrl"),
                likesCount = ExtractFieldInt(json, "likesCount"),
                updatedAt = ExtractFieldString(json, "updatedAt"),
                cardIds = ExtractArrayStrings(json, "cardIds"),
                likedBy = ExtractArrayStrings(json, "likedBy")
            };
        }

        private static List<VitrineCloudData> ParseVitrinesListFromQuery(string json)
        {
            var list = new List<VitrineCloudData>();
            if (string.IsNullOrEmpty(json)) return list;

            var docs = Regex.Matches(json, "\"document\"\\s*:\\s*\\{([\\s\\S]*?)\"createTime\"");
            foreach (Match m in docs)
            {
                string block = m.Value;
                var v = ParseVitrineDoc(block);
                if (v != null && !string.IsNullOrEmpty(v.userId))
                {
                    list.Add(v);
                }
            }
            return list;
        }

        private static string ExtractDocumentFields(string block)
        {
            var m = Regex.Match(block, "\"fields\"\\s*:\\s*\\{([\\s\\S]*?)\\}");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static List<string> ExtractArrayStrings(string json, string fieldName)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(json)) return list;

            var arrayMatch = Regex.Match(json, $"\"{fieldName}\"\\s*:\\s*\\{{\\s*\"arrayValue\"\\s*:\\s*\\{{\\s*\"values\"\\s*:\\s*\\[([\\s\\S]*?)\\]");
            if (!arrayMatch.Success) return list;

            string valuesBlock = arrayMatch.Groups[1].Value;
            var strMatches = Regex.Matches(valuesBlock, "\"stringValue\"\\s*:\\s*\"([^\"]*)\"");
            foreach (Match m in strMatches)
            {
                list.Add(m.Groups[1].Value);
            }
            return list;
        }

        // =========================================================================
        // HELPERS HTTP UNITYWEBREQUEST
        // =========================================================================

        private static async Task<string> PostJsonAsync(string url, string jsonBody, string bearerToken)
        {
            return await SendRequestAsync(url, "POST", jsonBody, bearerToken);
        }

        private static async Task<string> PatchJsonAsync(string url, string jsonBody, string bearerToken)
        {
            return await SendRequestAsync(url, "PATCH", jsonBody, bearerToken);
        }

        private static async Task<string> GetJsonAsync(string url, string bearerToken)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                if (!string.IsNullOrEmpty(bearerToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + bearerToken);
                }

                var tcs = new TaskCompletionSource<string>();
                var op = req.SendWebRequest();
                op.completed += _ =>
                {
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        tcs.SetResult(req.downloadHandler.text);
                    }
                    else
                    {
                        string err = req.downloadHandler != null ? req.downloadHandler.text : req.error;
                        tcs.SetResult(err);
                    }
                };

                return await tcs.Task;
            }
        }

        private static async Task<string> SendRequestAsync(string url, string method, string jsonBody, string bearerToken)
        {
            using (var req = new UnityWebRequest(url, method))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(jsonBody ?? "");
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(bearerToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + bearerToken);
                }

                var tcs = new TaskCompletionSource<string>();
                var op = req.SendWebRequest();
                op.completed += _ =>
                {
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        tcs.SetResult(req.downloadHandler.text);
                    }
                    else
                    {
                        string err = req.downloadHandler != null ? req.downloadHandler.text : req.error;
                        tcs.SetResult(err);
                    }
                };

                string response = await tcs.Task;

                // Auto-renovar token si expiró (HTTP 401 Unauthorized)
                if (req.responseCode == 401 && !url.Contains("securetoken") && !url.Contains("identitytoolkit"))
                {
                    if (FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(FirebaseAuthManager.Instance.RefreshToken))
                    {
                        string newToken = await FirebaseAuthManager.Instance.EnsureValidTokenAsync();
                        if (!string.IsNullOrEmpty(newToken) && newToken != bearerToken)
                        {
                            return await SendRequestAsync(url, method, jsonBody, newToken);
                        }
                    }
                }

                return response;
            }
        }

        // =========================================================================
        // PARSERS LIGEROS
        // =========================================================================

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        public static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : "";
        }

        public static int ExtractJsonInt(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*([0-9]+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int val)) return val;
            return 0;
        }

        private static FirestoreUserData ParseUserDocFromQuery(string json)
        {
            if (!json.Contains("\"document\"")) return null;

            string uid = ExtractFieldString(json, "uid");
            if (string.IsNullOrEmpty(uid))
            {
                var match = Regex.Match(json, "\"name\"\\s*:\\s*\"[^\"]*/users/([^\"]+)\"");
                if (match.Success) uid = match.Groups[1].Value;
            }

            return new FirestoreUserData
            {
                uid = uid,
                displayName = ExtractFieldString(json, "displayName"),
                friendCode = ExtractFieldString(json, "friendCode"),
                photoUrl = ExtractFieldString(json, "photoUrl"),
                level = Mathf.Max(1, ExtractFieldInt(json, "level")),
                collectionPower = ExtractFieldInt(json, "collectionPower"),
                albumProgress = ExtractFieldInt(json, "albumProgress"),
                coins = ExtractFieldInt(json, "coins"),
                ownedCards = ParseOwnedCardsMap(json),
                friends = ParseFriendsArray(json),
                ideal11 = ParseStringArray(json, "ideal11"),
                featuredCards = ParseStringArray(json, "featuredCards")
            };
        }

        private static Dictionary<string, int> ParseOwnedCardsMap(string json)
        {
            var map = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(json)) return map;

            var blockMatch = Regex.Match(json, "\"ownedCards\"\\s*:\\s*\\{\\s*\"mapValue\"\\s*:\\s*\\{\\s*\"fields\"\\s*:\\s*\\{([\\s\\S]*?)\\}\\s*\\}");
            if (!blockMatch.Success) return map;

            string fieldsBlock = blockMatch.Groups[1].Value;
            var matches = Regex.Matches(fieldsBlock, "\"([^\"]+)\"\\s*:\\s*\\{\\s*\"integerValue\"\\s*:\\s*\"?([0-9]+)\"?");
            foreach (Match m in matches)
            {
                string cardId = m.Groups[1].Value;
                if (int.TryParse(m.Groups[2].Value, out int count))
                {
                    map[cardId] = count;
                }
            }
            return map;
        }

        private static List<FriendData> ParseFriendsArray(string json)
        {
            var list = new List<FriendData>();
            if (string.IsNullOrEmpty(json) || !json.Contains("\"friends\"")) return list;

            var arrayMatch = Regex.Match(json, "\"friends\"\\s*:\\s*\\{\\s*\"arrayValue\"\\s*:\\s*\\{\\s*\"values\"\\s*:\\s*\\[([\\s\\S]*?)\\]\\s*\\}");
            if (!arrayMatch.Success) return list;

            string valuesBlock = arrayMatch.Groups[1].Value;
            var maps = Regex.Matches(valuesBlock, "\\{\"mapValue\"\\s*:\\s*\\{\\s*\"fields\"\\s*:\\s*\\{([\\s\\S]*?)\\}\\s*\\}");
            foreach (Match m in maps)
            {
                string fields = m.Groups[1].Value;
                list.Add(new FriendData
                {
                    friendUid = ExtractFieldString(fields, "friendUid"),
                    displayName = ExtractFieldString(fields, "displayName"),
                    friendCode = ExtractFieldString(fields, "friendCode"),
                    level = Mathf.Max(1, ExtractFieldInt(fields, "level")),
                    collectionPower = ExtractFieldInt(fields, "collectionPower"),
                    albumProgress = ExtractFieldInt(fields, "albumProgress")
                });
            }
            return list;
        }

        private static List<FriendRequestData> ParseFriendRequests(string json)
        {
            var list = new List<FriendRequestData>();
            if (string.IsNullOrEmpty(json)) return list;

            var docIndices = new List<int>();
            int idx = json.IndexOf("\"document\"");
            while (idx >= 0)
            {
                docIndices.Add(idx);
                idx = json.IndexOf("\"document\"", idx + 10);
            }

            for (int i = 0; i < docIndices.Count; i++)
            {
                int start = docIndices[i];
                int end = (i + 1 < docIndices.Count) ? docIndices[i + 1] : json.Length;
                string block = json.Substring(start, end - start);

                string reqId = ExtractFieldString(block, "requestId");
                string fromUid = ExtractFieldString(block, "fromUid");
                string fromName = ExtractFieldString(block, "fromName");
                string fromCode = ExtractFieldString(block, "fromCode");
                string fromPhoto = ExtractFieldString(block, "fromPhotoUrl");
                string toUid = ExtractFieldString(block, "toUid");
                string toName = ExtractFieldString(block, "toName");
                string toCode = ExtractFieldString(block, "toCode");
                string status = ExtractFieldString(block, "status");
                string created = ExtractFieldString(block, "createdAt");

                if (!string.IsNullOrEmpty(reqId) && !string.IsNullOrEmpty(fromUid))
                {
                    list.Add(new FriendRequestData
                    {
                        requestId = reqId,
                        fromUid = fromUid,
                        fromName = !string.IsNullOrEmpty(fromName) ? fromName : "Entrenador",
                        fromCode = fromCode,
                        fromPhotoUrl = fromPhoto,
                        toUid = toUid,
                        toName = !string.IsNullOrEmpty(toName) ? toName : "Entrenador",
                        toCode = toCode,
                        status = status,
                        createdAt = created
                    });
                }
            }

            return list;
        }

        private static string ExtractFieldString(string json, string fieldName)
        {
            var match = Regex.Match(json, $"\"{fieldName}\"\\s*:\\s*\\{{\\s*\"stringValue\"\\s*:\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : "";
        }

        private static int ExtractFieldInt(string json, string fieldName)
        {
            var match = Regex.Match(json, $"\"{fieldName}\"\\s*:\\s*\\{{\\s*\"integerValue\"\\s*:\\s*\"?([0-9]+)\"?");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int val)) return val;
            return 0;
        }

        private static List<string> ParseStringArray(string json, string fieldName)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(json) || !json.Contains($"\"{fieldName}\"")) return list;

            var arrayMatch = Regex.Match(json, $"\"{fieldName}\"\\s*:\\s*\\{{\\s*\"arrayValue\"\\s*:\\s*\\{{\\s*\"values\"\\s*:\\s*\\[([\\s\\S]*?)\\]\\s*\\}}");
            if (!arrayMatch.Success) return list;

            string valuesBlock = arrayMatch.Groups[1].Value;
            var matches = Regex.Matches(valuesBlock, "\\{\"stringValue\"\\s*:\\s*\"([^\"]*)\"\\}");
            foreach (Match m in matches)
            {
                list.Add(m.Groups[1].Value);
            }
            return list;
        }

        private static List<TradeOfferItem> ParseTradeOffers(string json, bool isIncoming)
        {
            var list = new List<TradeOfferItem>();
            if (string.IsNullOrEmpty(json)) return list;

            var docIndices = new List<int>();
            int idx = json.IndexOf("\"document\"");
            while (idx >= 0)
            {
                docIndices.Add(idx);
                idx = json.IndexOf("\"document\"", idx + 10);
            }

            for (int i = 0; i < docIndices.Count; i++)
            {
                int start = docIndices[i];
                int end = (i + 1 < docIndices.Count) ? docIndices[i + 1] : json.Length;
                string block = json.Substring(start, end - start);

                string tradeId = ExtractFieldString(block, "tradeId");
                if (string.IsNullOrEmpty(tradeId)) continue;

                list.Add(new TradeOfferItem
                {
                    tradeId = tradeId,
                    fromUid = ExtractFieldString(block, "fromUid"),
                    fromDisplayName = ExtractFieldString(block, "fromDisplayName"),
                    toUid = ExtractFieldString(block, "toUid"),
                    toDisplayName = ExtractFieldString(block, "toDisplayName"),
                    offeredCardId = ExtractFieldString(block, "offeredCardId"),
                    offeredCardName = ExtractFieldString(block, "offeredCardName"),
                    offeredRarity = ExtractFieldString(block, "offeredRarity"),
                    requestedCardId = ExtractFieldString(block, "requestedCardId"),
                    requestedCardName = ExtractFieldString(block, "requestedCardName"),
                    requestedRarity = ExtractFieldString(block, "requestedRarity"),
                    status = ExtractFieldString(block, "status"),
                    timeAgo = "Reciente",
                    isIncoming = isIncoming
                });
            }
            return list;
        }

        private static List<MarketListingData> ParseMarketListings(string json, string myUid)
        {
            var list = new List<MarketListingData>();
            if (string.IsNullOrEmpty(json)) return list;

            var docIndices = new List<int>();
            int idx = json.IndexOf("\"document\"");
            while (idx >= 0)
            {
                docIndices.Add(idx);
                idx = json.IndexOf("\"document\"", idx + 10);
            }

            for (int i = 0; i < docIndices.Count; i++)
            {
                int start = docIndices[i];
                int end = (i + 1 < docIndices.Count) ? docIndices[i + 1] : json.Length;
                string block = json.Substring(start, end - start);

                string listingId = ExtractFieldString(block, "listingId");
                if (string.IsNullOrEmpty(listingId)) continue;

                string sellerUid = ExtractFieldString(block, "sellerUid");

                list.Add(new MarketListingData
                {
                    listingId = listingId,
                    sellerUid = sellerUid,
                    sellerDisplayName = ExtractFieldString(block, "sellerDisplayName"),
                    cardId = ExtractFieldString(block, "cardId"),
                    cardName = ExtractFieldString(block, "cardName"),
                    initials = ExtractFieldString(block, "initials"),
                    rarity = ExtractFieldString(block, "rarity"),
                    quantity = Mathf.Max(1, ExtractFieldInt(block, "quantity")),
                    pricePerCard = ExtractFieldInt(block, "pricePerCard"),
                    status = ExtractFieldString(block, "status"),
                    buyerUid = ExtractFieldString(block, "buyerUid"),
                    buyerDisplayName = ExtractFieldString(block, "buyerDisplayName"),
                    timeAgo = "Reciente",
                    isMine = !string.IsNullOrEmpty(myUid) && sellerUid == myUid
                });
            }
            return list;
        }
    }
}
