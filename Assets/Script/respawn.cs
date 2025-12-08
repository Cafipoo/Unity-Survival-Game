using UnityEngine;

[RequireComponent(typeof(Collider))]
public class respawn : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Référence directe au GameObject du joueur (recommandé). Si laissé vide, cherchera automatiquement.")]
    public GameObject playerObject;
    
    [Tooltip("Tag du joueur à détecter (utilisé seulement si playerObject n'est pas assigné)")]
    public string playerTag = "";
    
    [Tooltip("Utiliser la physique 2D (Collider2D) au lieu de 3D")]
    public bool use2DPhysics = false;
    
    [Header("Options de visibilité")]
    [Tooltip("Désactiver complètement le GameObject au lieu de juste le rendre invisible")]
    public bool disableGameObject = false;
    
    [Tooltip("Désactiver aussi le collider après récupération")]
    public bool disableCollider = true;
    
    private bool hasBeenCollected = false;
    private Renderer[] renderers;
    private Collider ownCollider;
    private Collider2D ownCollider2D;
    
    void Start()
    {
        // Récupérer tous les Renderers (pour rendre invisible)
        renderers = GetComponentsInChildren<Renderer>();
        
        // Si le joueur n'est pas assigné, le chercher automatiquement
        if (playerObject == null)
        {
            // Chercher le joueur par son script NewMonoBehaviourScript
            NewMonoBehaviourScript playerScript = FindObjectOfType<NewMonoBehaviourScript>();
            if (playerScript != null)
            {
                playerObject = playerScript.gameObject;
                Debug.Log($"[respawn] {gameObject.name} : Joueur trouvé automatiquement : {playerObject.name}");
            }
            else
            {
                Debug.LogWarning($"[respawn] {gameObject.name} : Aucun joueur trouvé ! Assurez-vous d'assigner le GameObject du joueur dans l'inspecteur.");
            }
        }
        
        // Récupérer les colliders
        if (use2DPhysics)
        {
            ownCollider2D = GetComponent<Collider2D>();
            if (ownCollider2D != null && !ownCollider2D.isTrigger)
            {
                ownCollider2D.isTrigger = true;
            }
        }
        else
        {
            ownCollider = GetComponent<Collider>();
            if (ownCollider != null && !ownCollider.isTrigger)
            {
                ownCollider.isTrigger = true;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (use2DPhysics || hasBeenCollected) return;
        
        if (IsPlayer(other))
        {
            CollectRespawn();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!use2DPhysics || hasBeenCollected) return;
        
        if (IsPlayer2D(other))
        {
            CollectRespawn();
        }
    }
    
    private void CollectRespawn()
    {
        if (hasBeenCollected) return; // Éviter les appels multiples
        
        hasBeenCollected = true;
        
        Debug.Log($"✅ Point de respawn récupéré : {gameObject.name}");
        
        // Rendre invisible ou désactiver
        if (disableGameObject)
        {
            // Désactiver complètement le GameObject
            gameObject.SetActive(false);
        }
        else
        {
            // Rendre invisible en désactivant tous les Renderers
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }
        
        // Désactiver le collider si demandé
        if (disableCollider)
        {
            if (ownCollider != null)
            {
                ownCollider.enabled = false;
            }
            if (ownCollider2D != null)
            {
                ownCollider2D.enabled = false;
            }
        }
    }
    
    private bool IsPlayer(Collider other)
    {
        // Priorité 1 : Vérifier si c'est le GameObject du joueur assigné directement
        if (playerObject != null)
        {
            // Vérifier si le collider appartient au joueur ou à ses enfants
            if (other.gameObject == playerObject || other.transform.IsChildOf(playerObject.transform))
            {
                return true;
            }
            // Vérifier aussi si le collider est attaché au Rigidbody du joueur
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == playerObject)
            {
                return true;
            }
        }
        
        // Priorité 2 : Si un tag est défini, vérifier le tag
        if (!string.IsNullOrEmpty(playerTag))
        {
            try
            {
                return other.CompareTag(playerTag);
            }
            catch
            {
                // Si le tag n'existe pas, continuer avec les autres vérifications
            }
        }
        
        // Priorité 3 : Vérifier si c'est le joueur en cherchant le script NewMonoBehaviourScript
        NewMonoBehaviourScript playerScript = other.GetComponent<NewMonoBehaviourScript>();
        if (playerScript == null)
        {
            playerScript = other.GetComponentInParent<NewMonoBehaviourScript>();
        }
        if (playerScript == null && other.attachedRigidbody != null)
        {
            playerScript = other.attachedRigidbody.GetComponent<NewMonoBehaviourScript>();
        }
        
        return playerScript != null;
    }
    
    private bool IsPlayer2D(Collider2D other)
    {
        // Priorité 1 : Vérifier si c'est le GameObject du joueur assigné directement
        if (playerObject != null)
        {
            // Vérifier si le collider appartient au joueur ou à ses enfants
            if (other.gameObject == playerObject || other.transform.IsChildOf(playerObject.transform))
            {
                return true;
            }
            // Vérifier aussi si le collider est attaché au Rigidbody2D du joueur
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == playerObject)
            {
                return true;
            }
        }
        
        // Priorité 2 : Si un tag est défini, vérifier le tag
        if (!string.IsNullOrEmpty(playerTag))
        {
            try
            {
                return other.CompareTag(playerTag);
            }
            catch
            {
                // Si le tag n'existe pas, continuer avec les autres vérifications
            }
        }
        
        // Priorité 3 : Vérifier si c'est le joueur en cherchant le script NewMonoBehaviourScript
        NewMonoBehaviourScript playerScript = other.GetComponent<NewMonoBehaviourScript>();
        if (playerScript == null)
        {
            playerScript = other.GetComponentInParent<NewMonoBehaviourScript>();
        }
        if (playerScript == null && other.attachedRigidbody != null)
        {
            playerScript = other.attachedRigidbody.GetComponent<NewMonoBehaviourScript>();
        }
        
        return playerScript != null;
    }
    
    // Méthode publique pour réinitialiser le point de respawn (utile pour les tests ou le restart)
    public void ResetRespawn()
    {
        hasBeenCollected = false;
        
        // Réactiver les Renderers
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
        
        // Réactiver les colliders
        if (ownCollider != null)
        {
            ownCollider.enabled = true;
        }
        if (ownCollider2D != null)
        {
            ownCollider2D.enabled = true;
        }
        
        // Réactiver le GameObject si nécessaire
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
}
