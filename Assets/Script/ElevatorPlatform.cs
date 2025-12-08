using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ElevatorPlatform : MonoBehaviour
{
    [Header("Paramètres de l'ascenseur")]
    [Tooltip("Hauteur cible à laquelle la plateforme doit monter (en unités Unity)")]
    public float targetHeight = 10f;
    
    [Tooltip("Vitesse de montée de la plateforme")]
    public float moveSpeed = 2f;
    
    [Tooltip("Tag du joueur à détecter (laisse vide pour détecter automatiquement)")]
    public string playerTag = "";
    
    [Tooltip("Retourner à la position initiale quand le joueur quitte la plateforme")]
    public bool returnToStart = false;
    
    [Tooltip("Vitesse de descente quand le joueur quitte (si returnToStart est activé)")]
    public float returnSpeed = 2f;
    
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private bool isPlayerOnPlatform = false;
    private bool isMoving = false;
    private Transform playerTransform;
    private Collider platformCollider;
    
    void Start()
    {
        // Sauvegarder la position initiale
        initialPosition = transform.position;
        
        // Calculer la position cible (même X et Z, mais Y à la hauteur cible)
        targetPosition = new Vector3(initialPosition.x, targetHeight, initialPosition.z);
        
        // Récupérer le collider
        platformCollider = GetComponent<Collider>();
        if (platformCollider == null)
        {
            Debug.LogError($"[ElevatorPlatform] Aucun Collider trouvé sur {gameObject.name}!");
        }
        
        Debug.Log($"[ElevatorPlatform] {gameObject.name} initialisé - Position initiale: {initialPosition}, Hauteur cible: {targetHeight}");
    }
    
    void Update()
    {
        if (isPlayerOnPlatform)
        {
            // Faire monter la plateforme vers la hauteur cible
            MovePlatformUp();
            
            // Déplacer le joueur avec la plateforme
            if (playerTransform != null)
            {
                // Le joueur sera déplacé automatiquement s'il est enfant de la plateforme
                // Sinon, on peut le déplacer manuellement ici si nécessaire
            }
        }
        else if (returnToStart && isMoving)
        {
            // Faire redescendre la plateforme à sa position initiale
            MovePlatformDown();
        }
    }
    
    private void MovePlatformUp()
    {
        // Calculer la direction vers la position cible
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // Si on n'a pas encore atteint la hauteur cible
        if (distance > 0.1f)
        {
            // Déplacer la plateforme vers la hauteur cible
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            isMoving = true;
        }
        else
        {
            // On a atteint la hauteur cible
            transform.position = targetPosition;
            isMoving = false;
        }
    }
    
    private void MovePlatformDown()
    {
        // Calculer la direction vers la position initiale
        float distance = Vector3.Distance(transform.position, initialPosition);
        
        // Si on n'a pas encore atteint la position initiale
        if (distance > 0.1f)
        {
            // Déplacer la plateforme vers la position initiale
            transform.position = Vector3.MoveTowards(transform.position, initialPosition, returnSpeed * Time.deltaTime);
        }
        else
        {
            // On a atteint la position initiale
            transform.position = initialPosition;
            isMoving = false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerOnPlatform = true;
            playerTransform = other.transform;
            Debug.Log($"🚀 [ElevatorPlatform] {gameObject.name} : Joueur détecté ! Montée vers la hauteur {targetHeight}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerOnPlatform = false;
            playerTransform = null;
            Debug.Log($"⬇️ [ElevatorPlatform] {gameObject.name} : Joueur a quitté la plateforme");
            
            if (returnToStart)
            {
                Debug.Log($"   Retour à la position initiale activé");
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (IsPlayer(collision.collider))
        {
            isPlayerOnPlatform = true;
            playerTransform = collision.transform;
            Debug.Log($"🚀 [ElevatorPlatform] {gameObject.name} : Joueur détecté (collision) ! Montée vers la hauteur {targetHeight}");
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        if (IsPlayer(collision.collider))
        {
            isPlayerOnPlatform = false;
            playerTransform = null;
            Debug.Log($"⬇️ [ElevatorPlatform] {gameObject.name} : Joueur a quitté la plateforme (collision)");
            
            if (returnToStart)
            {
                Debug.Log($"   Retour à la position initiale activé");
            }
        }
    }
    
    private bool IsPlayer(Collider other)
    {
        // Si un tag est défini, vérifier le tag
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
        
        // Sinon, vérifier si c'est le joueur en cherchant le script NewMonoBehaviourScript
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
    
    // Méthode pour définir la hauteur cible depuis l'inspecteur ou le code
    public void SetTargetHeight(float height)
    {
        targetHeight = height;
        targetPosition = new Vector3(initialPosition.x, targetHeight, initialPosition.z);
        Debug.Log($"[ElevatorPlatform] {gameObject.name} : Hauteur cible changée à {height}");
    }
    
    // Méthode pour réinitialiser la plateforme à sa position initiale
    public void ResetPlatform()
    {
        transform.position = initialPosition;
        isPlayerOnPlatform = false;
        isMoving = false;
        playerTransform = null;
    }
}

