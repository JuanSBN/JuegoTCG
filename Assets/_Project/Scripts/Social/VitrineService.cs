using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using JuegoTCG.Networking;
using JuegoTCG.Cards;

namespace JuegoTCG.Social
{
    /// <summary>
    /// Servicio central para el Sistema de Vitrinas Públicas (Showcase).
    /// Permite a los jugadores seleccionar sus mejores cartas para exhibirlas,
    /// explorar vitrinas populares y de amigos, y dar 'Like' persistente en Cloud Firestore.
    /// </summary>
    public class VitrineService : MonoBehaviour
    {
        public static VitrineService Instance { get; private set; }

        public event Action OnVitrinesUpdated;
        public event Action<VitrineCloudData> OnMyVitrineUpdated;

        private VitrineCloudData myVitrine;
        private readonly List<VitrineCloudData> popularVitrines = new List<VitrineCloudData>();
        private readonly List<VitrineCloudData> friendVitrines = new List<VitrineCloudData>();

        public VitrineCloudData MyVitrine => myVitrine;
        public IReadOnlyList<VitrineCloudData> PopularVitrines => popularVitrines;
        public IReadOnlyList<VitrineCloudData> FriendVitrines => friendVitrines;

        private const string PREF_MY_VITRINE_CARDS = "Vitrine_MyCardIds";

        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var existing = FindFirstObjectByType<VitrineService>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    GameObject go = new GameObject("VitrineService");
                    Instance = go.AddComponent<VitrineService>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            await RefreshVitrinesAsync();
        }

        /// <summary>
        /// Recarga las vitrinas públicas desde Firestore y sincroniza la vitrina propia y de amigos.
        /// </summary>
        public async Task RefreshVitrinesAsync()
        {
            if (FirebaseAuthManager.Instance == null || !FirebaseAuthManager.Instance.IsAuthenticated)
            {
                popularVitrines.Clear();
                friendVitrines.Clear();
                OnVitrinesUpdated?.Invoke();
                return;
            }

            string token = FirebaseAuthManager.Instance.IdToken;
            string myUid = FirebaseAuthManager.Instance.UserId;

            try
            {
                // 1. Cargar vitrina propia
                if (!string.IsNullOrEmpty(myUid))
                {
                    var myDoc = await FirebaseRestClient.GetVitrineDocAsync(token, myUid);
                    if (myDoc != null)
                    {
                        myVitrine = myDoc;
                    }
                    else
                    {
                        // Si aún no tiene vitrina en la nube, crear borrador inicial con sus mejores cartas y publicarlo
                        myVitrine = CreateDefaultLocalDraft(myUid);
                        if (myVitrine.cardIds != null && myVitrine.cardIds.Count > 0)
                        {
                            _ = SaveMyVitrineAsync(myVitrine.cardIds);
                        }
                    }
                    OnMyVitrineUpdated?.Invoke(myVitrine);
                }

                // 2. Cargar vitrinas populares desde Firestore
                var cloudVitrines = await FirebaseRestClient.GetPublicVitrinesAsync(token, 20);
                popularVitrines.Clear();

                if (cloudVitrines != null && cloudVitrines.Count > 0)
                {
                    popularVitrines.AddRange(cloudVitrines);
                }

                // 3. Cargar vitrinas de amigos desde Firestore
                friendVitrines.Clear();
                SocialService.EnsureExists();
                if (SocialService.Instance != null)
                {
                    if (SocialService.Instance.Friends == null || SocialService.Instance.Friends.Count == 0)
                    {
                        await SocialService.Instance.RefreshCloudRequestsAndFriendsAsync();
                    }

                    var friends = SocialService.Instance.Friends;
                    if (friends != null)
                    {
                        foreach (var f in friends)
                        {
                            if (string.IsNullOrEmpty(f.friendUid)) continue;

                            // Si ya vino en populares
                            var fVitrine = popularVitrines.Find(v => v.userId == f.friendUid);
                            if (fVitrine != null)
                            {
                                friendVitrines.Add(fVitrine);
                            }
                            else
                            {
                                // Consultar directamente la vitrina del amigo
                                var friendDoc = await FirebaseRestClient.GetVitrineDocAsync(token, f.friendUid);
                                if (friendDoc != null && friendDoc.cardIds != null && friendDoc.cardIds.Count > 0)
                                {
                                    friendVitrines.Add(friendDoc);
                                }
                            }
                        }
                    }
                }

                OnVitrinesUpdated?.Invoke();
                Debug.Log($"<color=green>[VitrineService] Vitrinas actualizadas: {popularVitrines.Count} populares, {friendVitrines.Count} de amigos.</color>");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VitrineService] Error refrescando vitrinas: {ex.Message}");
                popularVitrines.Clear();
                friendVitrines.Clear();
                OnVitrinesUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Guarda y publica las cartas elegidas para la vitrina del jugador actual en Firestore.
        /// </summary>
        public async Task<bool> SaveMyVitrineAsync(List<string> cardIds)
        {
            if (FirebaseAuthManager.Instance == null || !FirebaseAuthManager.Instance.IsAuthenticated)
            {
                Debug.LogWarning("[VitrineService] Usuario no autenticado para guardar vitrina.");
                return false;
            }

            string token = FirebaseAuthManager.Instance.IdToken;
            string uid = FirebaseAuthManager.Instance.UserId;
            string name = FirebaseAuthManager.Instance.DisplayName;
            string code = FirebaseAuthManager.Instance.FriendCode;
            string avatar = GetUserInitials(name);
            string photo = FirebaseAuthManager.Instance.PhotoUrl;
            int likes = myVitrine != null ? myVitrine.likesCount : 0;
            List<string> likedBy = myVitrine != null ? myVitrine.likedBy : new List<string>();

            // Guardar localmente en PlayerPrefs
            string joined = string.Join(",", cardIds);
            PlayerPrefs.SetString(GetPrefKey(), joined);
            PlayerPrefs.Save();

            bool ok = await FirebaseRestClient.UpsertVitrineDocAsync(
                token, uid, name, code, avatar, photo, cardIds, likes, likedBy);

            if (ok)
            {
                myVitrine = new VitrineCloudData
                {
                    userId = uid,
                    displayName = name,
                    friendCode = code,
                    avatarText = avatar,
                    photoUrl = photo,
                    cardIds = new List<string>(cardIds),
                    likesCount = likes,
                    likedBy = likedBy,
                    updatedAt = DateTime.UtcNow.ToString("o")
                };

                // Actualizar o añadir a la lista popular
                int idx = popularVitrines.FindIndex(v => v.userId == uid);
                if (idx >= 0) popularVitrines[idx] = myVitrine;
                else popularVitrines.Insert(0, myVitrine);

                OnMyVitrineUpdated?.Invoke(myVitrine);
                OnVitrinesUpdated?.Invoke();
                Debug.Log($"<color=green>[VitrineService] ¡Vitrina propia publicada con éxito con {cardIds.Count} cartas!</color>");
            }
            return ok;
        }

        /// <summary>
        /// Alterna el estado de 'Like' sobre una vitrina y lo persiste en Firestore.
        /// </summary>
        public async Task<bool> ToggleLikeAsync(string targetUserId)
        {
            if (FirebaseAuthManager.Instance == null || !FirebaseAuthManager.Instance.IsAuthenticated) return false;

            string token = FirebaseAuthManager.Instance.IdToken;
            string myUid = FirebaseAuthManager.Instance.UserId;

            VitrineCloudData target = popularVitrines.Find(v => v.userId == targetUserId)
                                   ?? friendVitrines.Find(v => v.userId == targetUserId)
                                   ?? (myVitrine != null && myVitrine.userId == targetUserId ? myVitrine : null);

            if (target == null) return false;

            bool isCurrentlyLiked = target.IsLikedBy(myUid);
            int prevLikes = target.likesCount;
            var prevLikedBy = new List<string>(target.likedBy ?? new List<string>());

            // Actualización optimista inmediata en memoria
            if (isCurrentlyLiked)
            {
                target.likedBy.Remove(myUid);
                target.likesCount = Mathf.Max(0, target.likesCount - 1);
            }
            else
            {
                if (!target.likedBy.Contains(myUid)) target.likedBy.Add(myUid);
                target.likesCount++;
            }

            OnVitrinesUpdated?.Invoke();

            // Sincronización en la nube vía REST
            bool ok = await FirebaseRestClient.ToggleVitrineLikeAsync(
                token, targetUserId, myUid, isCurrentlyLiked, prevLikes, prevLikedBy);

            if (!ok)
            {
                // Revertir en caso de error
                target.likesCount = prevLikes;
                target.likedBy = prevLikedBy;
                OnVitrinesUpdated?.Invoke();
                Debug.LogWarning("[VitrineService] Falló sincronización de like en Firestore.");
            }

            return ok;
        }

        private VitrineCloudData CreateDefaultLocalDraft(string uid)
        {
            string name = FirebaseAuthManager.Instance.DisplayName;
            string code = FirebaseAuthManager.Instance.FriendCode;
            string avatar = GetUserInitials(name);
            string photo = FirebaseAuthManager.Instance.PhotoUrl;

            var defaultCardIds = new List<string>();

            // Si hay guardadas en PlayerPrefs:
            string saved = PlayerPrefs.GetString(GetPrefKey(), "");
            if (!string.IsNullOrEmpty(saved))
            {
                defaultCardIds.AddRange(saved.Split(','));
            }
            else
            {
                // Si no, tomar las mejores cartas que posea el usuario
                if (PlayerCollectionManager.Instance != null)
                {
                    var map = PlayerCollectionManager.Instance.GetOwnedCardsMap();
                    foreach (var kvp in map)
                    {
                        if (kvp.Value > 0 && defaultCardIds.Count < 6)
                        {
                            defaultCardIds.Add(kvp.Key);
                        }
                    }
                }
            }

            return new VitrineCloudData
            {
                userId = uid,
                displayName = name,
                friendCode = code,
                avatarText = avatar,
                photoUrl = photo,
                cardIds = defaultCardIds,
                likesCount = 0,
                likedBy = new List<string>(),
                updatedAt = DateTime.UtcNow.ToString("o")
            };
        }


        private string GetPrefKey()
        {
            string uid = FirebaseAuthManager.Instance != null && !string.IsNullOrEmpty(FirebaseAuthManager.Instance.UserId)
                ? FirebaseAuthManager.Instance.UserId
                : "local";
            return $"Vitrine_{uid}_CardIds";
        }

        private string GetUserInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "YO";
            string[] parts = name.Trim().Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            }
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }
    }
}
