using UnityEngine;

public class MovingCube : MonoBehaviour
{
    [Header("Points de déplacement")]
    [Tooltip("Méthode pour définir les points : Transform (GameObjects) ou Coordonnées directes")]
    public PointDefinitionMethod pointMethod = PointDefinitionMethod.Coordinates;
    
    [Tooltip("Point de départ (Point A) - Transform. Utilisé seulement si pointMethod = Transform")]
    public Transform pointA;
    
    [Tooltip("Point d'arrivée (Point B) - Transform. Utilisé seulement si pointMethod = Transform")]
    public Transform pointB;
    
    [Tooltip("Coordonnées du point de départ (Point A). Utilisé seulement si pointMethod = Coordinates")]
    public Vector3 pointACoordinates = Vector3.zero;
    
    [Tooltip("Coordonnées du point d'arrivée (Point B). Utilisé seulement si pointMethod = Coordinates")]
    public Vector3 pointBCoordinates = Vector3.zero;
    
    public enum PointDefinitionMethod
    {
        Transform,      // Utiliser des GameObjects Transform
        Coordinates     // Utiliser des coordonnées Vector3 directes
    }
    
    [Header("Paramètres de mouvement")]
    [Tooltip("Vitesse de déplacement du cube")]
    public float moveSpeed = 3f;
    
    [Tooltip("Type de mouvement")]
    public MovementType movementType = MovementType.PingPong;
    
    [Tooltip("Délai avant de commencer le mouvement (en secondes)")]
    public float startDelay = 0f;
    
    [Tooltip("Délai à chaque point avant de repartir (en secondes)")]
    public float waitTimeAtPoints = 0f;
    
    [Header("Options")]
    [Tooltip("Démarrer le mouvement automatiquement au démarrage")]
    public bool startOnAwake = true;
    
    private Vector3 startPosition;
    private Vector3 endPosition;
    private Vector3 currentTarget;
    private bool isMoving = false;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float delayTimer = 0f;
    
    public enum MovementType
    {
        PingPong,    // Va de A à B puis revient à A (boucle)
        Loop,        // Va de A à B puis téléporte à A (boucle)
        OneWay       // Va de A à B puis s'arrête
    }
    
    void Start()
    {
        // Définir les positions de départ et d'arrivée
        SetupPositions();
        
        // Initialiser la position
        if (pointMethod == PointDefinitionMethod.Transform && pointA != null)
        {
            transform.position = pointA.position;
        }
        else if (pointMethod == PointDefinitionMethod.Coordinates && pointACoordinates != Vector3.zero)
        {
            transform.position = pointACoordinates;
        }
        
        // Définir la cible initiale
        currentTarget = endPosition;
        
        // Démarrer le mouvement si demandé
        if (startOnAwake)
        {
            delayTimer = startDelay;
        }
    }
    
    void Update()
    {
        // Gérer le délai de démarrage
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }
        
        // Si on attend à un point
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                isMoving = true;
            }
            return;
        }
        
        // Si on est en mouvement
        if (isMoving)
        {
            MoveCube();
        }
    }
    
    private void SetupPositions()
    {
        if (pointMethod == PointDefinitionMethod.Transform)
        {
            // Utiliser les Transform (GameObjects)
            if (pointA != null)
            {
                startPosition = pointA.position;
            }
            else
            {
                startPosition = transform.position;
            }
            
            if (pointB != null)
            {
                endPosition = pointB.position;
            }
            else
            {
                Debug.LogError($"[MovingCube] {gameObject.name} : Point B (Transform) n'est pas assigné ! Le cube ne peut pas bouger.");
                enabled = false;
                return;
            }
        }
        else // Coordinates
        {
            // Utiliser les coordonnées directes
            if (pointACoordinates == Vector3.zero && pointBCoordinates == Vector3.zero)
            {
                // Si aucune coordonnée n'est définie, utiliser la position actuelle comme point A
                startPosition = transform.position;
                Debug.LogWarning($"[MovingCube] {gameObject.name} : Aucune coordonnée définie. Utilisation de la position actuelle comme point A.");
            }
            else
            {
                startPosition = pointACoordinates;
            }
            
            if (pointBCoordinates == Vector3.zero)
            {
                Debug.LogError($"[MovingCube] {gameObject.name} : Point B (Coordonnées) n'est pas défini ! Le cube ne peut pas bouger.");
                enabled = false;
                return;
            }
            
            endPosition = pointBCoordinates;
        }
        
        isMoving = true;
    }
    
    private void MoveCube()
    {
        // Calculer la direction et la distance vers la cible
        Vector3 direction = (currentTarget - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, currentTarget);
        
        // Si on est proche de la cible (à moins de 0.1 unité)
        if (distance < 0.1f)
        {
            // Arriver exactement à la position cible
            transform.position = currentTarget;
            
            // Gérer le comportement selon le type de mouvement
            HandleReachedTarget();
        }
        else
        {
            // Déplacer vers la cible
            transform.position = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);
        }
    }
    
    private void HandleReachedTarget()
    {
        // Attendre si nécessaire
        if (waitTimeAtPoints > 0f)
        {
            isWaiting = true;
            waitTimer = waitTimeAtPoints;
            isMoving = false;
        }
        
        // Gérer le type de mouvement
        switch (movementType)
        {
            case MovementType.PingPong:
                // Inverser la direction (ping-pong)
                if (currentTarget == endPosition)
                {
                    currentTarget = startPosition;
                }
                else
                {
                    currentTarget = endPosition;
                }
                break;
                
            case MovementType.Loop:
                // Téléporter au point A et repartir vers B
                transform.position = startPosition;
                currentTarget = endPosition;
                break;
                
            case MovementType.OneWay:
                // S'arrêter
                isMoving = false;
                break;
        }
        
        // Reprendre le mouvement après l'attente (si on attend)
        if (!isWaiting)
        {
            isMoving = true;
        }
    }
    
    // Méthode publique pour démarrer le mouvement
    public void StartMovement()
    {
        isMoving = true;
        delayTimer = 0f;
        waitTimer = 0f;
        isWaiting = false;
    }
    
    // Méthode publique pour arrêter le mouvement
    public void StopMovement()
    {
        isMoving = false;
    }
    
    // Méthode publique pour réinitialiser la position
    public void ResetPosition()
    {
        transform.position = startPosition;
        currentTarget = endPosition;
        isMoving = false;
        isWaiting = false;
        delayTimer = startDelay;
    }
    
    // Méthode publique pour changer la vitesse
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    // Dessiner les points A et B dans l'éditeur (Gizmos)
    void OnDrawGizmos()
    {
        Vector3 posA, posB;
        
        if (pointMethod == PointDefinitionMethod.Transform)
        {
            posA = pointA != null ? pointA.position : transform.position;
            posB = pointB != null ? pointB.position : transform.position;
        }
        else // Coordinates
        {
            posA = pointACoordinates != Vector3.zero ? pointACoordinates : transform.position;
            posB = pointBCoordinates != Vector3.zero ? pointBCoordinates : transform.position;
        }
        
        // Dessiner le point A en vert
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(posA, 0.5f);
        Gizmos.DrawLine(posA, posA + Vector3.up * 2f);
        
        // Dessiner le point B en rouge
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(posB, 0.5f);
        Gizmos.DrawLine(posB, posB + Vector3.up * 2f);
        
        // Dessiner la ligne entre A et B
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(posA, posB);
    }
    
    void OnDrawGizmosSelected()
    {
        Vector3 posA, posB;
        
        if (pointMethod == PointDefinitionMethod.Transform)
        {
            posA = pointA != null ? pointA.position : transform.position;
            posB = pointB != null ? pointB.position : transform.position;
        }
        else // Coordinates
        {
            posA = pointACoordinates != Vector3.zero ? pointACoordinates : transform.position;
            posB = pointBCoordinates != Vector3.zero ? pointBCoordinates : transform.position;
        }
        
        // Dessiner une flèche pour montrer la direction
        Vector3 direction = (posB - posA).normalized;
        Vector3 arrowPos = posA + direction * (Vector3.Distance(posA, posB) * 0.5f);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(arrowPos, arrowPos + direction * 2f);
    }
}

