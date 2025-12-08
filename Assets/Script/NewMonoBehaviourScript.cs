using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer = -1; // Tous les layers par défaut
    public float mouseSensitivity = 2f;
    public Camera playerCamera;
    public float maxLookAngle = 80f; // Angle maximum pour regarder vers le haut/bas
    public int maxHealth = 100; // Points de vie maximum
    
    [Header("Animation Settings")]
    [Tooltip("Activer ou désactiver les animations du joueur")]
    public bool enableAnimations = true;
    
    [Header("Shooting Settings")]
    public GameObject ballPrefab; // Le prefab de la balle à tirer
    public float shootVelocity = 20f; // Vitesse de la balle
    public float shootCooldown = 0.5f; // Temps entre chaque tir
    public float spawnDistance = 1.5f; // Distance devant le joueur pour spawner la balle
    
    [Header("Game Over")]
    public GameObject gameOverCanvas; // Le canvas d'écran de fin à afficher
    [Tooltip("Tag des surfaces létales qui tuent instantanément le joueur (ex: KillZone)")]
    public string lethalSurfaceTag = "KillZone";
    [Tooltip("Liste optionnelle de surfaces létales précises (colliders). Si renseignée, seul un contact avec ces surfaces tue.")]
    public Collider[] lethalSurfaces;
    
    [Header("Checkpoint / Respawn")]
    [Tooltip("Liste optionnelle de volumes de checkpoint. Si renseignée, un contact avec ces volumes définira le point de respawn.")]
    public Collider[] checkpointVolumes;
    [Tooltip("Détecter les checkpoints par nom (cherche 'Checkpoint' dans le nom de l'objet). Activé par défaut.")]
    public bool detectCheckpointByName = true;
    [Tooltip("Tag des checkpoints (ex: Checkpoint). Laisser vide pour utiliser uniquement la détection par nom. DÉSACTIVÉ par défaut.")]
    public string checkpointTag = "";
    [Tooltip("Tag des points de respawn (ex: Respawn). Si laissé vide, cherchera des GameObjects avec 'Respawn' ou 'SpawnPoint' dans le nom.")]
    public string respawnPointTag = "Respawn";
    [Tooltip("Liste optionnelle de points de respawn assignés manuellement. Si laissée vide, cherchera automatiquement dans la scène.")]
    public Transform[] respawnPoints;

    private Rigidbody rb;
    private bool isGrounded;
    private Collider col;
    private float verticalRotation = 0f;
    private int currentHealth; // Points de vie actuels
    private float lastShootTime = 0f; // Temps du dernier tir
    private bool isDead = false; // État de mort du joueur
    private Vector3 startPosition; // Position de départ du joueur
    private Quaternion startRotation; // Rotation de départ du joueur
    private Vector3 respawnPosition; // Position du dernier checkpoint
    private Quaternion respawnRotation; // Rotation du dernier checkpoint
    private Animator animator; // Référence à l'Animator pour bloquer les animations
    private bool wasKinematic; // Sauvegarder l'état kinematic du Rigidbody
    private playerScriptAnim playerAnimScript; // Référence au script d'animation du joueur
    private System.Collections.Generic.List<Transform> allRespawnPoints = new System.Collections.Generic.List<Transform>(); // Liste de tous les points de respawn trouvés

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // S'assurer que le Rigidbody peut bouger librement en Y
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        col = GetComponent<Collider>();
        
        // Chercher l'Animator pour gérer les animations
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Chercher le script d'animation du joueur
        playerAnimScript = GetComponent<playerScriptAnim>();
        if (playerAnimScript == null)
        {
            playerAnimScript = GetComponentInChildren<playerScriptAnim>();
        }
        
        // Appliquer l'état initial des animations
        UpdateAnimationState();
        
        // Sauvegarder l'état kinematic initial du Rigidbody
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
        }
        
        // Sauvegarder la position et rotation de départ
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        // Chercher tous les points de respawn dans la scène
        FindAllRespawnPoints();
        
        // Vérifier d'abord si un point de respawn a déjà été activé (via le script RespawnPoint)
        if (RespawnPoint.HasActivatedRespawnPoint())
        {
            respawnPosition = RespawnPoint.GetLastRespawnPosition();
            respawnRotation = RespawnPoint.GetLastRespawnRotation();
            Debug.Log($"Point de respawn initialisé depuis le dernier checkpoint activé : {respawnPosition}");
        }
        else
        {
            // Chercher d'abord "respawn lvl 0" comme point de respawn par défaut
            Transform defaultRespawn = FindRespawnByName("respawn lvl 0");
            
            // Si "respawn lvl 0" n'est pas trouvé, utiliser le point le plus proche
            Transform nearestRespawn = defaultRespawn != null ? defaultRespawn : FindNearestRespawnPoint(startPosition);
            
            // Initialiser respawnPosition avec le point trouvé ou la position de départ
            respawnPosition = nearestRespawn != null ? nearestRespawn.position : startPosition;
            respawnRotation = nearestRespawn != null ? nearestRespawn.rotation : startRotation;
            
            if (defaultRespawn != null)
            {
                Debug.Log($"Point de respawn par défaut utilisé : {defaultRespawn.name}");
            }
        }
        
        // Chercher la caméra si elle n'est pas assignée
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
        
        // Verrouiller et cacher le curseur pour le contrôle FPS
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialiser les points de vie
        currentHealth = maxHealth;
        
        // Désactiver le canvas de game over au démarrage s'il est assigné
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }
    }

    // Méthode pour mettre à jour l'état des animations
    private void UpdateAnimationState()
    {
        // Activer/désactiver l'Animator selon la case à cocher
        if (animator != null)
        {
            animator.enabled = enableAnimations;
        }
    }
    
    // Appelé quand les valeurs changent dans l'inspecteur
    void OnValidate()
    {
        // Mettre à jour l'état des animations si le script est déjà initialisé
        if (animator != null)
        {
            UpdateAnimationState();
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // Si le joueur est mort, ne pas permettre les contrôles
        if (isDead)
        {
            return;
        }
        
        // Rotation de la caméra avec la souris
        HandleMouseLook();

        // Vérification si l'objet est au sol
        CheckGrounded();

        // Saut avec Espace
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
            // Déclencher l'animation de saut qui va forcer l'arrêt des autres animations
            if (enableAnimations && playerAnimScript != null)
            {
                playerAnimScript.PlayJumpAnimation();
            }
        }
        
        // Permettre de déverrouiller le curseur avec Échap
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Gérer le clic gauche : tirer si le curseur est verrouillé, sinon verrouiller le curseur
        if (Input.GetMouseButtonDown(0))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // Tirer une balle
                Shoot();
            }
            else
            {
                // Re-verrouiller le curseur
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void FixedUpdate()
    {
        // Si le joueur est mort, ne rien faire (le Rigidbody est déjà en kinematic)
        if (isDead)
        {
            return;
        }
        
        // Récupération des entrées WASD
        float horizontal = Input.GetAxis("Horizontal"); // A/D ou Flèches gauche/droite
        float vertical = Input.GetAxis("Vertical");     // W/S ou Flèches haut/bas

        // Calcul du mouvement relatif à la direction de la caméra
        // Utiliser la direction du joueur (transform) pour le mouvement
        Vector3 moveDirection = transform.forward * vertical + transform.right * horizontal;
        moveDirection.Normalize(); // Normaliser pour éviter un mouvement plus rapide en diagonale
        
        // Appliquer la vitesse et conserver la vélocité Y pour la gravité/saut
        Vector3 movement = moveDirection * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
        
        // Détecter le mouvement et déclencher l'animation de marche
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);
        if (enableAnimations && isMoving && isGrounded && playerAnimScript != null)
        {
            playerAnimScript.PlayWalkAnimation();
        }
    }

    void CheckGrounded()
    {
        // Calculer le point de départ du raycast (bas du collider ou centre si pas de collider)
        Vector3 rayStart = transform.position;
        if (col != null)
        {
            rayStart = col.bounds.center;
            rayStart.y = col.bounds.min.y; // Bas du collider
        }

        // Raycast vers le bas pour vérifier si on est au sol
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer);
        
        // Debug pour voir le raycast dans l'éditeur
        Debug.DrawRay(rayStart, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    void Jump()
    {
        // Application de la force de saut
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
    }

    void HandleMouseLook()
    {
        // Ne gérer la rotation que si le curseur est verrouillé
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // Récupération des entrées de la souris
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotation horizontale (Y axis) - fait tourner le joueur
        transform.Rotate(0f, mouseX, 0f);

        // Rotation verticale (X axis) - fait tourner la caméra vers le haut/bas
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);

        // Appliquer la rotation verticale à la caméra
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    // Méthode pour prendre des dégâts
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // S'assurer que les PV ne descendent pas en dessous de 0
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Méthode pour se soigner
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // S'assurer que les PV ne dépassent pas le maximum
    }

    // Méthode appelée quand le joueur meurt
    void Die()
    {
        if (isDead) return; // Éviter d'appeler plusieurs fois
        
        isDead = true;
        
        // Arrêter complètement le mouvement et bloquer le Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // Mettre le Rigidbody en kinematic pour bloquer complètement les mouvements
            rb.isKinematic = true;
        }
        
        // Bloquer les animations - désactiver l'Animator ou jouer l'animation de mort
        if (animator != null)
        {
            // Essayer de jouer l'animation de mort si elle existe
            if (animator.parameters != null)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.name == "defeatedTrigger" || param.name == "Defeated")
                    {
                        animator.SetTrigger(param.name);
                        break;
                    }
                }
            }
            // Désactiver l'Animator pour bloquer toutes les animations
            animator.enabled = false;
        }
        
        // Désactiver le collider pour éviter les collisions
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Déverrouiller le curseur pour permettre de cliquer sur les boutons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Afficher l'écran de fin
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            Debug.Log("Écran de fin affiché!");
        }
        else
        {
            Debug.LogWarning("Aucun canvas de game over assigné! Assignez-le dans l'inspecteur Unity.");
        }
        
        Debug.Log("Le joueur est mort! Mouvements et animations bloqués.");
    }

    // Getter pour obtenir les PV actuels
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // Getter pour obtenir les PV maximum
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    // Méthode publique pour réinitialiser complètement le joueur
    public void ResetPlayer()
    {
        // Réinitialiser l'état de mort
        isDead = false;
        
        // Réinitialiser la santé
        currentHealth = maxHealth;
        
        // Réactiver le collider
        if (col != null)
        {
            col.enabled = true;
        }
        
        // Réactiver le Rigidbody et restaurer son état kinematic
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Réactiver l'Animator selon l'état de enableAnimations
        if (animator != null)
        {
            animator.enabled = enableAnimations;
            // Réinitialiser tous les triggers de l'Animator (seulement si activé)
            if (enableAnimations && animator.parameters != null)
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        animator.ResetTrigger(param.name);
                    }
                }
            }
        }
        
        // TOUJOURS vérifier le dernier point de respawn activé (via le script RespawnPoint) en priorité
        if (RespawnPoint.HasActivatedRespawnPoint())
        {
            Vector3 lastRespawnPos = RespawnPoint.GetLastRespawnPosition();
            Quaternion lastRespawnRot = RespawnPoint.GetLastRespawnRotation();
            
            // Vérifier que la position n'est pas Vector3.zero (qui indiquerait un problème)
            if (lastRespawnPos != Vector3.zero)
            {
                respawnPosition = lastRespawnPos;
                respawnRotation = lastRespawnRot;
                Debug.Log($"🔄 Respawn du joueur au dernier checkpoint activé : {respawnPosition}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Le dernier checkpoint activé a une position invalide (Vector3.zero). Utilisation de la position sauvegardée: {respawnPosition}");
            }
        }
        else
        {
            Debug.Log($"🔄 Aucun checkpoint activé. Respawn du joueur à la position sauvegardée: {respawnPosition}");
        }
        
        // Réinitialiser la position et rotation au point de respawn (checkpoint ou respawn par défaut)
        transform.position = respawnPosition;
        transform.rotation = respawnRotation;
        
        // Réinitialiser la rotation verticale de la caméra
        verticalRotation = 0f;
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.identity;
        }
        
        // Réinitialiser le curseur
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Désactiver le canvas de game over
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }
        
        // Réinitialiser le temps de tir
        lastShootTime = 0f;
        
        Debug.Log($"Joueur réinitialisé au respawn ({respawnPosition})");
    }
    
    // Méthode pour tirer une balle
    void Shoot()
    {
        // Vérifier le cooldown
        if (Time.time - lastShootTime < shootCooldown)
            return;
        
        // Vérifier qu'un prefab est assigné
        if (ballPrefab == null)
        {
            Debug.LogWarning("NewMonoBehaviourScript: Aucun prefab de balle assigné!");
            return;
        }
        
        // Utiliser la direction de la caméra pour déterminer où regarde le joueur
        Vector3 shootDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        
        // Calculer la position de spawn : devant le joueur avec un offset pour éviter les collisions
        Vector3 spawnPosition = transform.position + shootDirection * spawnDistance;
        
        // Ajuster la hauteur pour spawner à peu près au niveau de la caméra
        if (playerCamera != null)
        {
            spawnPosition.y = playerCamera.transform.position.y;
        }
        
        // Instancier la balle avec la rotation de la caméra (ou du joueur si pas de caméra)
        Quaternion spawnRotation = playerCamera != null ? playerCamera.transform.rotation : transform.rotation;
        GameObject newBall = Instantiate(ballPrefab, spawnPosition, spawnRotation);
        
        // Ajouter un Rigidbody si nécessaire
        Rigidbody ballRigidbody = newBall.GetComponent<Rigidbody>();
        if (ballRigidbody == null)
        {
            ballRigidbody = newBall.AddComponent<Rigidbody>();
        }
        
        // Ajouter le script Projectile si nécessaire (pour infliger des dégâts)
        Projectile projectile = newBall.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = newBall.AddComponent<Projectile>();
            projectile.damage = 25; // Les balles font 25 dégâts
        }
        
        // Appliquer la vélocité dans la direction où regarde le joueur
        ballRigidbody.linearVelocity = shootDirection * shootVelocity;
        
        // Mettre à jour le temps du dernier tir
        lastShootTime = Time.time;
        
        Debug.Log("Balle tirée dans la direction: " + shootDirection);
    }

    private bool TryHandleCheckpoint(Collider other)
    {
        if (other == null) return false;
        
        bool checkpointFound = false;
        
        // 1) Liste explicite de volumes de checkpoint
        if (checkpointVolumes != null && checkpointVolumes.Length > 0)
        {
            foreach (var cp in checkpointVolumes)
            {
                if (cp != null && cp == other)
                {
                    SetCheckpoint(other.transform);
                    checkpointFound = true;
                    break;
                }
            }
        }

        // 2) Par nom (si activé et pas encore trouvé) - PRIORITAIRE
        if (!checkpointFound && detectCheckpointByName)
        {
            string objName = other.gameObject.name.ToLower();
            if (objName.Contains("checkpoint"))
            {
                SetCheckpoint(other.transform);
                checkpointFound = true;
            }
        }

        // 3) Par tag (uniquement si le tag est défini ET que la détection par nom n'a rien trouvé)
        if (!checkpointFound && !string.IsNullOrEmpty(checkpointTag))
        {
            // Vérifier si le tag existe avant de l'utiliser
            if (TagExists(checkpointTag))
            {
                if (other.CompareTag(checkpointTag))
                {
                    SetCheckpoint(other.transform);
                    checkpointFound = true;
                }
            }
            // Si le tag n'existe pas, on ne fait rien (pas de warning car la détection par nom est prioritaire)
        }

        return checkpointFound;
    }
    
    // Vérifier si un tag existe dans Unity
    private bool TagExists(string tag)
    {
        try
        {
            // Essayer de trouver un GameObject avec ce tag
            // Si le tag n'existe pas, Unity lancera une exception
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetCheckpoint(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("SetCheckpoint appelé avec un Transform null!");
            return;
        }
        
        // Vérifier si ce Transform a un script RespawnPoint et l'activer
        RespawnPoint respawnPointScript = target.GetComponent<RespawnPoint>();
        if (respawnPointScript == null)
        {
            // Chercher dans les enfants
            respawnPointScript = target.GetComponentInChildren<RespawnPoint>();
        }
        
        // Si un RespawnPoint existe, l'utiliser (il mettra à jour respawnPosition automatiquement)
        if (respawnPointScript != null)
        {
            // Forcer l'activation du RespawnPoint
            respawnPointScript.SetAsLastRespawnPoint();
            // Mettre à jour respawnPosition depuis le RespawnPoint
            respawnPosition = RespawnPoint.GetLastRespawnPosition();
            respawnRotation = RespawnPoint.GetLastRespawnRotation();
            Debug.Log($"✅ Checkpoint atteint (avec RespawnPoint) : {target.name}");
        }
        else
        {
            // Mettre à jour le checkpoint manuellement
            Vector3 oldPosition = respawnPosition;
            respawnPosition = target.position;
            respawnRotation = target.rotation;
            Debug.Log($"✅ Checkpoint atteint : {target.name}");
            Debug.Log($"   Position précédente: {oldPosition}");
        }
        
        Debug.Log($"   Nouvelle position de respawn: {respawnPosition}");
    }
    
    // Chercher tous les points de respawn dans la scène
    private void FindAllRespawnPoints()
    {
        allRespawnPoints.Clear();
        
        // Ajouter les points de respawn assignés manuellement
        if (respawnPoints != null && respawnPoints.Length > 0)
        {
            foreach (Transform respawn in respawnPoints)
            {
                if (respawn != null && !allRespawnPoints.Contains(respawn))
                {
                    allRespawnPoints.Add(respawn);
                }
            }
        }
        
        // Chercher par tag
        if (!string.IsNullOrEmpty(respawnPointTag))
        {
            GameObject[] respawnObjects = GameObject.FindGameObjectsWithTag(respawnPointTag);
            foreach (GameObject obj in respawnObjects)
            {
                if (obj != null && !allRespawnPoints.Contains(obj.transform))
                {
                    allRespawnPoints.Add(obj.transform);
                }
            }
        }
        
        // Chercher par nom (Respawn ou SpawnPoint)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            if ((objName.Contains("respawn") || objName.Contains("spawnpoint") || objName.Contains("spawn_point")) 
                && !allRespawnPoints.Contains(obj.transform))
            {
                allRespawnPoints.Add(obj.transform);
            }
        }
        
        if (allRespawnPoints.Count > 0)
        {
            Debug.Log($"Trouvé {allRespawnPoints.Count} point(s) de respawn dans la scène.");
        }
        else
        {
            Debug.LogWarning("Aucun point de respawn trouvé dans la scène. Utilisation de la position de départ.");
        }
    }
    
    // Trouver un point de respawn par son nom (insensible à la casse)
    private Transform FindRespawnByName(string name)
    {
        string searchName = name.ToLower();
        
        foreach (Transform respawnPoint in allRespawnPoints)
        {
            if (respawnPoint == null) continue;
            
            if (respawnPoint.name.ToLower() == searchName)
            {
                return respawnPoint;
            }
        }
        
        return null;
    }
    
    // Trouver le point de respawn le plus proche d'une position donnée
    private Transform FindNearestRespawnPoint(Vector3 position)
    {
        if (allRespawnPoints.Count == 0)
        {
            return null;
        }
        
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Transform respawnPoint in allRespawnPoints)
        {
            if (respawnPoint == null) continue;
            
            float distance = Vector3.Distance(position, respawnPoint.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = respawnPoint;
            }
        }
        
        if (nearest != null)
        {
            Debug.Log($"Point de respawn le plus proche trouvé : {nearest.name} (distance: {nearestDistance:F2})");
        }
        
        return nearest;
    }

    private bool IsLethal(Collider other)
    {
        // 1) Si une liste explicite est fournie, on ne tue que si elle contient le collider touché.
        if (lethalSurfaces != null && lethalSurfaces.Length > 0)
        {
            foreach (var lethalCol in lethalSurfaces)
            {
                if (lethalCol != null && lethalCol == other)
                {
                    return true;
                }
            }
            // Pas trouvé dans la liste : on ignore.
            return false;
        }

        // 2) Sinon on tombe sur le comportement par tag.
        bool isLethal = string.IsNullOrEmpty(lethalSurfaceTag) || other.CompareTag(lethalSurfaceTag);
        return isLethal;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Détection des checkpoints puis des surfaces létales.
        if (isDead) return;

        // Priorité : si on touche un RespawnPoint, on met à jour immédiatement
        if (TryHandleRespawnPoint(other))
        {
            return;
        }

        // Priorité : mise à jour du checkpoint si on en touche un.
        if (TryHandleCheckpoint(other))
        {
            return;
        }

        // Ensuite, surfaces létales.
        if (IsLethal(other))
        {
            Die();
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Mettre à jour le checkpoint même si le joueur reste en contact
        if (isDead) return;
        
        // Mettre à jour si on reste en contact avec un RespawnPoint
        if (TryHandleRespawnPoint(other))
        {
            return;
        }

        // Mettre à jour le checkpoint si on reste en contact avec un checkpoint
        TryHandleCheckpoint(other);
    }

    // Détection directe des RespawnPoint (spheres)
    private bool TryHandleRespawnPoint(Collider other)
    {
        if (other == null) return false;

        RespawnPoint rp = other.GetComponent<RespawnPoint>();
        if (rp == null)
        {
            rp = other.GetComponentInParent<RespawnPoint>();
        }

        if (rp != null)
        {
            // Active le point et synchronise la position de respawn
            rp.SetAsLastRespawnPoint();
            respawnPosition = RespawnPoint.GetLastRespawnPosition();
            respawnRotation = RespawnPoint.GetLastRespawnRotation();
            Debug.Log($"🎯 RespawnPoint touché : {rp.gameObject.name} -> nouvelle position de respawn {respawnPosition}");
            return true;
        }

        return false;
    }
}
