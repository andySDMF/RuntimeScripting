using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.EventSystems;

#if BRANDLAB360_AVATARS_READYPLAYERME
using BrandLab360.ReadyPlayerMe;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrandLab360
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour, IPlayer
    {
        [Header("Cameras")]
        public GameObject playerCamera;
        public GameObject thirdPersonCamera;

        [Header("Product")]
        public GameObject ProductHolder;

        [Header("Pointer")]
        public GameObject pointer;

        [Header("Pointer")]
        public AudioSource waterSoundEffect;
        public AudioClip swimmingSound;

        [Header("Avatar")]
        public Animator animator;
        public Transform animatorHolder;
        public GameObject man;
        public GameObject woman;
        public GameObject simpleMan;
        public GameObject simpleWoman;
        public List<GameObject> customAvatars = new List<GameObject>();

        [Header("Movement")]
        public float walkSpeed = 4f;
        public float sprintSpeed = 8f;
        public bool jumpEnabled = false;
        public float jumpPower = 5f;
        public float maxSlopeAngle = 55;
        public float maxStepHeight = 0.3f;
        public float stepSmooth = 2f;

        [Header("Footsteps")]
        public AudioClip footstepClip;
        public float footstepVolume = 1.0f;
        public float footstepFrequency = 2.0f;

        [Header("Stamina")]
        public float staminaDepletionSpeed = 5f;
        public float staminaLevel = 50;

        [Header("FirstPerson")]
        public float verticalRotationRange = 170;
        public float mouseSensitivity = 5;
        public float fOVToMouseSensitivity = 1;
        public float cameraSmoothing = 5f;
        public bool invertMouseX = false;
        public bool invertMouseY = false;
        public float FOVKickAmount = 2.5f;
        public float changeTime = 0.75f;

        private CinemachineCamera m_playerCamera;
        private CameraThirdPerson m_ThirdPersonController;
        private Rigidbody m_rigidbody;
        private CapsuleCollider m_capsule;

        private float m_walkSpeed;
        private float m_sprintSpeed;
        private float m_jumpPower;
        private bool m_jump;
        private bool m_isJumping;
        private float m_jumpVelocity;
        private bool m_isSprinting = false;
        private bool m_topDownActive = false;

        private Vector3 m_originalRotation;
        private bool m_useStamina = false;

        private Image m_staminaMeter;
        private Image m_staminaMeterBG;

        private float m_CamFOV;
        private float m_cacheFOV;
        private float m_stamina;
        private int m_movement = 0;

        private float m_frameCount = 0;
        private float m_frameLimit = 10;
        private Vector3 m_lastPosition;
        private Quaternion m_lastRotation;

        private bool m_isMine = true;
        private bool m_canMove = true;
        private bool m_thirdPerson = false;
        private float m_elapsedTime = 0.0f;
        private int m_mouseButtonClick = 1;

        private Vector3 m_targetAngles;
        private Vector3 m_followAngles;
        private Vector3 m_followVelocity;
        private Vector2 m_inputDelta;

        private float m_staminaSmoothRef;
        private float m_speed;
        private float m_strafingSpeed = 1.0f;
        private float m_fovRef;

        //animation blending
        private float m_strifeVal = 0.0f;
        private float m_turnVal = 0.0f;
        private float m_moveVal = 0.0f;

        //keys
        private string m_forwardKey = "W";
        private string m_backKey = "S";
        private string m_leftKey = "A";
        private string m_rightKey = "D";
        private string m_sprintKey = "LeftShift";
        private string m_strifeLeftKey = "Z";
        private string m_strifeRightKey = "X";
        private string m_focusKey = "F";
        private string m_interactionKey = "Enter";

        //focus
        private bool m_wasInThirdPersonFocus = false;
        private float m_maxFocus = 10;
        private Coroutine m_focusProcess;
        private bool m_isFocused = false;

        //sound
        private AudioSource m_audioSource;
        private float m_footstep = 0.5f;
        private float m_footstepCycle = 0.0f;
        private Vector3 m_previousVelocity = Vector3.zero;
        private Vector3 m_previousPosition;

        private PlayerJoystickReader m_variableJoystick;

        private GameObject stepRayUpper;
        private GameObject stepRayLower;

        private bool m_isInWater = false;

        public float Speed { get { return m_speed; } }

        public System.Action<GameObject> OnAvatarCreated { get; set; }

        public bool IsGrounded { get; private set; }

        public GameObject MainProductHolder { get { return ProductHolder; } }

        public Animator Animation { get { return animator; } }

        public string SittingAnimation { get; set; }

        public GameObject Avatar { get { return animator.gameObject; } }

        public bool OverrideAnimations { get; set; }

        public bool IsInWater { get; set; }

        public WaterHandler WaterHandler { get; set; }

        public string NickName
        {
            get
            {
                if (gameObject == null) return "";

                if (!gameObject.activeInHierarchy) return "";

                if (GetComponent<MMOPlayer>() != null)
                {
                    return GetComponent<MMOPlayer>().Nickname;
                }

                return "";
            }
        }

        public string ID
        {
            get
            {
                if (gameObject == null) return "";

                if (!gameObject.activeInHierarchy) return "";

                if (GetComponent<MMOPlayer>() != null)
                {
                    return GetComponent<MMOPlayer>().ID;
                }

                return "You";
            }
        }

        public int ActorNumber
        {
            get
            {
                if (GetComponent<MMOPlayer>() != null)
                {
                    return GetComponent<MMOPlayer>().Actor;
                }

                return -1;
            }

        }

        public GameObject ArrowPointer { get { return pointer; } }

        public bool IsSprinting { get { return m_isSprinting; } }

        public GameObject MainCamera
        {
            get
            {
                if (playerCamera == null)
                {
                    return null;
                }

                if (!playerCamera.gameObject.activeInHierarchy)
                {
                    if (thirdPersonCamera.gameObject.activeInHierarchy || PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        return thirdPersonCamera;
                    }
                }

                return playerCamera;
            }
        }

        public GameObject MainObject { get { return gameObject; } }

        public Transform TransformObject { get { return transform; } }

        public bool FreezeRotation { get; set; }

        public bool FreezePosition { get; set; }

        public Vector2 TargetCameraRotation
        {
            get
            {
                return m_followAngles;
            }
            set
            {
                m_followAngles = new Vector3(value.x, value.y, m_followAngles.z);
                m_targetAngles = new Vector3(value.x, value.y, m_followAngles.z);
            }
        }

        public bool IsLocal
        {
            get
            {
                return m_isMine;
            }
            set
            {
                m_isMine = value;

                if(animatorHolder.transform.childCount > 0)
                {
                    string layer = (m_isMine) ? "Avatar" : "Default";

                    Avatar.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer(layer);

                    foreach (var child in Avatar.transform.GetChild(0).gameObject.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.layer = LayerMask.NameToLayer(layer);
                    }

                    animatorHolder.transform.localScale = Vector3.one;
                }
            }
        }

        public GameObject ThirdPerson
        {
            get
            {
                return thirdPersonCamera;
            }
        }

        public int RotationInput
        {
            get;
            private set;
        }

        public Hashtable CustomizationData { get; private set; }

        public bool OverrideAnimationHandler { get; set; }

        public UnityEngine.AI.NavMeshAgent NavMeshAgentScript { get; private set; }

        public bool IsButtonHeldDown { get; set; }

        public string InteractionKey
        {
            get
            {
                return m_interactionKey;
            }
        }

        public int MovementID
        {
            get;
            private set;
        }

        public bool IsMoving
        {
            get
            {
                if (FreezePosition)
                {
                    return false;
                }

                return Vector3.Distance(m_lastPosition, transform.position) > 1;
            }
        }

        private void Awake()
        {
            //network setup
            if (AppManager.Instance.Data.Mode.Equals(MultiplayerMode.Offline))
            {
                if (GetComponent<MMOPlayer>())
                {
                    Destroy(GetComponent<MMOPlayer>());
                }

                gameObject.name = gameObject.name + "_" + "You";
            }
            else
            {
                m_isMine = GetComponent<MMOView>().IsMine;
            }

            if (GetComponent<AudioSource>() == null) 
            {
                m_audioSource = gameObject.AddComponent<AudioSource>(); 
            }
            else
            {
                m_audioSource = GetComponent<AudioSource>();
            }

            m_variableJoystick = GetComponent<PlayerJoystickReader>();
            m_playerCamera = playerCamera.GetComponentInChildren<CinemachineCamera>(true);
            m_ThirdPersonController = thirdPersonCamera.GetComponentInChildren<CameraThirdPerson>(true);

            m_capsule = GetComponent<CapsuleCollider>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_rigidbody.interpolation = RigidbodyInterpolation.Extrapolate;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            IsGrounded = true;
            m_originalRotation = Vector3.zero;

            m_walkSpeed = walkSpeed;
            m_sprintSpeed = sprintSpeed;
            m_jumpPower = jumpPower;

            FreezePosition = true;
            FreezeRotation = true;
            RotationInput = 1;

            RotationInput = 1;
            OverrideAnimationHandler = false;

            bool createNavMeshAgent = m_isMine ? CoreManager.Instance.playerSettings.createNavMeshAgent : CoreManager.Instance.playerSettings.createRemoteNavMeshAgent;

            if (createNavMeshAgent)
            {
                NavMeshAgentScript = gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
                NavMeshAgentScript.radius = 0.3f;
                NavMeshAgentScript.avoidancePriority = 1;
                NavMeshAgentScript.stoppingDistance = 0;
            }

            CustomizationData = new Hashtable();
        }

        private void Start()
        {
            stepRayLower = new GameObject("Step Lower");
            stepRayLower.transform.SetParent(transform);
            stepRayLower.transform.localPosition = new Vector3(0, (0 - (m_capsule.height / 2)) + 0.05f, 0);
            stepRayLower.transform.localEulerAngles = transform.forward;
            stepRayLower.transform.localScale = Vector3.one;

            stepRayUpper = new GameObject("Step Upper");
            stepRayUpper.transform.SetParent(transform);
            stepRayUpper.transform.localPosition = new Vector3(0, stepRayLower.transform.localPosition.y + maxStepHeight, 0);
            stepRayUpper.transform.localEulerAngles = transform.forward;
            stepRayUpper.transform.localScale = Vector3.one;

            m_capsule.sharedMaterial = new PhysicMaterial("Zero_Friction");
            m_capsule.sharedMaterial.dynamicFriction = 0;
            m_capsule.sharedMaterial.staticFriction = 0;
            m_capsule.sharedMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
            m_capsule.sharedMaterial.bounceCombine = PhysicMaterialCombine.Minimum;

            PlayerManager.OnUpdate += OnThisUpdate;
            PlayerManager.OnFixedUpdate += OnThisFixedUpdate;

            m_previousPosition = m_rigidbody.position;

            m_useStamina = CoreManager.Instance.playerSettings.useStaminaBar;

            if (m_useStamina)
            {
                Transform stamina = HUDManager.Instance.GetMenuItem("Player_Stamina");

                if (stamina != null)
                {
                    m_staminaMeterBG = stamina.GetComponent<Image>();
                    m_staminaMeter = stamina.GetChild(0).GetComponent<Image>();
                }
            }
            else
            {
                Transform stamina = HUDManager.Instance.GetMenuItem("Player_Stamina");
                stamina.gameObject.SetActive(false);
            }

            m_CamFOV = m_playerCamera.Lens.FieldOfView;
            m_cacheFOV = m_CamFOV;

            m_capsule.radius = m_capsule.height / 8;
            m_stamina = staminaLevel;


            MovementID = -1;
            m_movement = MovementID;
        }

        private void OnDestroy()
        {
            PlayerManager.OnUpdate -= OnThisUpdate;
            PlayerManager.OnFixedUpdate -= OnThisFixedUpdate;
        }

        private void OnThisUpdate()
        {
            if (!m_isMine || !m_canMove) return;

            m_rigidbody.collisionDetectionMode = (FreezePosition) ? CollisionDetectionMode.Discrete : CollisionDetectionMode.ContinuousSpeculative;
            m_rigidbody.isKinematic = FreezePosition;

            HandleInput();
        }

        private void OnThisFixedUpdate()
        {
            if (!m_isMine || !m_canMove) return;

            if (!FreezePosition)
            {
                HandleStamina();
            }

            HandleMovement();
        }

        private void HandleMovement()
        {
            bool isMovingForawrd = false;
            bool isMovingBackwards = false;
            bool isStrafing = false;
            bool isTurning = false;

            if (!FreezePosition && !m_variableJoystick.isMoving)
            {
                float horizontalInput = 0;
                float verticalInput = 0;

                if(IsInWater)
                {
                    isStrafing = false;
                    m_strifeVal = 0.0f;

                    if (InputManager.Instance.GetKey(m_forwardKey) || InputManager.Instance.GetKey("UpArrow"))
                    {
                        verticalInput = 1;
                        m_movement = (m_isSprinting) ? 1 : 0;
                    }
                    else
                    {
                        m_moveVal = 0.0f;
                    }

                    if (InputManager.Instance.GetKey(m_backKey) || InputManager.Instance.GetKey("DownArrow"))
                    {
                        m_isSprinting = false;
                        m_movement = (m_isSprinting) ? 1 : 0;

                        if (verticalInput > 0)
                        {
                            //need to angle player down 45 degrees
                        }
                        else
                        {
                            //need to angle player down 80 degrees
                        }
                    }
                }
                else
                {
                    if (InputManager.Instance.GetKey(m_forwardKey) || InputManager.Instance.GetKey("UpArrow"))
                    {
                        verticalInput = 1;
                        m_movement = (m_isSprinting) ? 1 : 0;
                    }
                    else if (InputManager.Instance.GetKey(m_backKey) || InputManager.Instance.GetKey("DownArrow"))
                    {
                        verticalInput = -1;
                        m_isSprinting = false;
                        m_movement = (m_isSprinting) ? 1 : 0;
                    }
                    else
                    {
                        m_moveVal = 0.0f;
                    }

                    if (verticalInput == 0)
                    {
                        if (InputManager.Instance.GetKey(m_strifeLeftKey))
                        {
                            horizontalInput = -1;
                            m_movement = 2;
                            isStrafing = true;
                        }
                        else if (InputManager.Instance.GetKey(m_strifeRightKey))
                        {
                            horizontalInput = 1;
                            m_movement = 3;
                            isStrafing = true;
                        }
                    }
                    else
                    {
                        isStrafing = false;
                        m_strifeVal = 0.0f;
                    }
                }

                m_inputDelta = new Vector2(horizontalInput, verticalInput);

                if (m_inputDelta.magnitude > 1) { m_inputDelta.Normalize(); }

                if (verticalInput > 0)
                {
                    isMovingForawrd = true;
                    RotationInput = 1;
                }
                else if (verticalInput < 0)
                {
                    isMovingBackwards = true;
                    RotationInput = -1;
                }

                float rotationInput = 0;

                if (InputManager.Instance.GetKey(m_leftKey) || InputManager.Instance.GetKey("LeftArrow"))
                {
                    rotationInput = -1;

                    if (verticalInput == 0)
                    {
                        m_movement = 4;
                        isTurning = true;
                    }
                    else
                    {
                        m_turnVal = 0.0f;
                    }
                }
                else if (InputManager.Instance.GetKey(m_rightKey) || InputManager.Instance.GetKey("RightArrow"))
                {
                    rotationInput = 1;

                    if (verticalInput == 0)
                    {
                        m_movement = 5;
                        isTurning = true;
                    }
                    else
                    {
                        m_turnVal = 0.0f;
                    }
                }
                else
                {
                    m_turnVal = 0.0f;
                }

                RotatePlayer(rotationInput, isTurning);
            }


            Vector3 MoveDirection = Vector3.zero;
            m_speed = m_isSprinting ? m_sprintSpeed : isStrafing ? m_strafingSpeed : isTurning ? m_walkSpeed / 2 : m_walkSpeed;


            if(!FreezePosition)
            {
                m_rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;

                if (maxSlopeAngle > 0)
                {
                    MoveDirection = ((transform.forward * m_inputDelta.y * m_speed + transform.right * m_inputDelta.x * (isStrafing ? m_strafingSpeed : (isTurning ? m_walkSpeed / 2 : m_walkSpeed))) * (m_rigidbody.velocity.y > 0.01f ? 0.4f : 0.8f));
                }
                else
                {
                    MoveDirection = (transform.forward * m_inputDelta.y * m_speed + transform.right * m_inputDelta.x * (isStrafing ? m_strafingSpeed : (isTurning ? m_walkSpeed / 2 : m_walkSpeed)));
                }
            }

            MoveDirection +=  HandleStep(MoveDirection);

            m_jumpVelocity = m_rigidbody.velocity.y;

            if (IsGrounded && m_jump && m_jumpPower > 0 && !m_isJumping && !FreezePosition)
            {
                if (maxSlopeAngle > 0)
                {
                    m_isJumping = true;
                    m_jump = false;
                    m_jumpVelocity += m_rigidbody.velocity.y < 0.01f ? m_jumpPower : m_jumpPower / 3;
                    m_rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                }
                else
                {
                    m_isJumping = true;
                    m_jump = false;
                    m_jumpVelocity += m_jumpPower;
                }
            }

            /*if (maxSlopeAngle > 0)
            {
                if (!m_isJumping)
                {
                    m_jumpVelocity += Physics.gravity.y;
                }
            }*/

            if (!m_rigidbody.isKinematic)
            {
                if (!FreezePosition)
                {
                    m_rigidbody.velocity = MoveDirection + (Vector3.up * m_jumpVelocity);
                }
                else
                {
                    m_rigidbody.velocity = Vector3.zero;
                }
            }

            m_rigidbody.AddForce(Physics.gravity * (1.0f - 1));


            if (FOVKickAmount > 0)
            {
                if (m_isSprinting && m_playerCamera.Lens.FieldOfView != (m_CamFOV + (FOVKickAmount * 2) - 0.01f))
                {
                    if (Mathf.Abs(m_rigidbody.velocity.x) > 0.5f || Mathf.Abs(m_rigidbody.velocity.z) > 0.5f)
                    {
                        m_playerCamera.Lens.FieldOfView = Mathf.SmoothDamp(m_playerCamera.Lens.FieldOfView, m_CamFOV + (FOVKickAmount * 2), ref m_fovRef, changeTime);
                    }

                }
                else if (m_playerCamera.Lens.FieldOfView != m_CamFOV) 
                { 
                    m_playerCamera.Lens.FieldOfView = Mathf.SmoothDamp(m_playerCamera.Lens.FieldOfView, m_CamFOV, ref m_fovRef, changeTime * 0.5f); 
                }
            }


            bool ismoving = m_inputDelta.magnitude > 0.0f;

            if(!IsInWater)
            {
                HandleFootsteps(ismoving);
            }
            

            IsGrounded = false;

            if (animator != null)
            {
                animator.transform.localPosition = Vector3.zero;

                if (!FreezeRotation)
                {
                    animator.transform.localEulerAngles = Vector3.zero;

                    if (!NavigationManager.Instance.Mode.Equals(NavigationMode.Desktop))
                    {
                        if (!m_variableJoystick.isMoving)
                        {
                            RotateAvatar(isMovingForawrd, isMovingBackwards);
                        }
                    }
                    else
                    {
                        RotateAvatar(isMovingForawrd, isMovingBackwards);
                    }
                }
            }


            HandleAnimation();
        }

        private Vector3 HandleStep(Vector3 direction)
        {
           // Debug.DrawRay(stepRayLower.transform.position, direction, Color.red, 0, false);
           // Debug.DrawRay(stepRayUpper.transform.position, direction, Color.red, 0, false);

            RaycastHit hitLower;
            if (Physics.Raycast(stepRayLower.transform.position, direction, out hitLower, m_capsule.radius + 0.2f))
            {
                RaycastHit hitUpper;
                if (!Physics.Raycast(stepRayUpper.transform.position, direction, out hitUpper, m_capsule.radius + 0.6f))
                {
                    return new Vector3(0, maxStepHeight * 1.2f, 0);
                }
            }


            //this is for angle step, ignore for now

             /*RaycastHit hitLower45;
             if (Physics.Raycast(stepRayLower.transform.position, direction, out hitLower45, m_capsule.radius + 0.2f))
             {

                 RaycastHit hitUpper45;
                 if (!Physics.Raycast(stepRayUpper.transform.position, direction, out hitUpper45, m_capsule.radius + 0.6f))
                 {
                    return new Vector3(0, maxStepHeight * 1.2f, 0);
                }
             }

             RaycastHit hitLowerMinus45;
             if (Physics.Raycast(stepRayLower.transform.position, direction, out hitLowerMinus45, m_capsule.radius + 0.2f))
             {

                 RaycastHit hitUpperMinus45;
                 if (!Physics.Raycast(stepRayUpper.transform.position, direction, out hitUpperMinus45, m_capsule.radius + 0.6f))
                 {
                    return new Vector3(0, maxStepHeight * 1.2f, 0);
                }
             }*/

            return Vector3.zero;
        }

        private void HandleInput()
        {
            if (!m_thirdPerson)
            {
                if (!FreezeRotation && !EventSystem.current.IsPointerOverGameObject())
                {
                    bool mouseButton = (!CoreManager.Instance.IsMobile) ? InputManager.Instance.GetMouseButton(m_mouseButtonClick) : InputManager.Instance.GetMouseButton(0);

                    if (!m_variableJoystick.isMoving)
                    {
                        bool isTurning = false;
                        float mouseYInput = 0;
                        float mouseXInput = 0;
                        float camFOV = m_playerCamera.Lens.FieldOfView;

                        int n = 0;

                        if (InputManager.Instance.GetKey("NumpadPlus") || InputManager.Instance.GetKey("Equals"))
                        {
                            n = AppManager.Instance.Settings.playerSettings.invertArrowPadLook ? -1 : 1;
                        }
                        else if (InputManager.Instance.GetKey("NumpadMinus") || InputManager.Instance.GetKey("Minus"))
                        {
                            n = AppManager.Instance.Settings.playerSettings.invertArrowPadLook ? 1 : -1;
                        }

                        mouseYInput = n * -1;

                        HandleCamera(mouseXInput, mouseYInput, isTurning, camFOV);

                        if (mouseButton)
                        {
                            Vector2 delta = InputManager.Instance.GetMouseDelta("Mouse X", "Mouse Y");

                            mouseYInput = !invertMouseY ? delta.y / 10 : -delta.y / 10;
                            mouseXInput = !invertMouseX ? delta.x / 10 : -delta.x / 10;

                            if (mouseYInput != 0 || mouseXInput != 0)
                            {
                                IsButtonHeldDown = true;
                            }

                            //check if moving forward
                            if (m_inputDelta.normalized.y.Equals(0))
                            {
                                if (mouseXInput >= 0)
                                {
                                    isTurning = true;
                                    m_movement = 4;
                                }
                                else if (mouseXInput < 0)
                                {
                                    m_movement = 5;
                                    isTurning = true;
                                }
                                else
                                {
                                    isTurning = true;
                                    m_movement = -1;
                                    m_turnVal = 0.0f;
                                }
                            }

                            HandleCamera(mouseXInput, mouseYInput, isTurning, camFOV);
                        }
                        else
                        {
                            if (IsButtonHeldDown)
                            {
                                //need to wait a frame until button can be released- this is for the navigation manager
                                if (m_elapsedTime < 0.2f)
                                {
                                    m_elapsedTime += Time.deltaTime;
                                }
                                else
                                {
                                    m_elapsedTime = 0.0f;
                                    IsButtonHeldDown = false;
                                }
                            }
                            else
                            {
                                IsButtonHeldDown = false;
                            }
                        }
                    }
                    else
                    {
                        IsButtonHeldDown = true;
                    }
                }
            }

            if (!FreezePosition)
            {
                if ((InputManager.Instance.GetKeyDown("Space") && jumpEnabled))
                {
                    m_jump = true;
                }
                else if (InputManager.Instance.GetKeyUp("Space"))
                {
                    m_jump = false;
                }

                //focus input
                if (AppManager.Instance.Settings.HUDSettings.showFocusToggle)
                {
                    if (InputManager.Instance.GetKeyUp(m_focusKey))
                    {
                        PlayerManager.Instance.focusUIToogle.GetComponent<Toggle>().isOn = !m_isFocused;
                    }

                    if (m_isFocused && PlayerManager.Instance.ThirdPersonCameraActive)
                    {
                        PlayerManager.Instance.focusUIToogle.GetComponent<Toggle>().isOn = false;

                        if (m_focusProcess != null)
                        {
                            StopCoroutine(m_focusProcess);
                        }

                        m_isFocused = false;
                        m_focusProcess = null;
                        m_CamFOV = m_cacheFOV;
                    }
                }
            }
        }

        private void HandleStamina()
        {
            if (m_useStamina)
            {
                m_isSprinting = InputManager.Instance.GetKey(m_sprintKey) && m_stamina > 0 && (Mathf.Abs(m_rigidbody.velocity.x) > 0.01f || Mathf.Abs(m_rigidbody.velocity.z) > 0.01f);

                if (m_inputDelta.normalized.y <= 0)
                {
                    m_isSprinting = false;
                }

                if (m_isSprinting)
                {
                    m_ThirdPersonController.ResetActiveCameraControl();

                    if (m_useStamina)
                    {
                        m_stamina -= (staminaDepletionSpeed * 2) * Time.deltaTime;

                        m_staminaMeterBG.color = Vector4.MoveTowards(m_staminaMeterBG.color, new Vector4(0, 0, 0, 0.5f), 0.15f);
                        m_staminaMeter.color = Vector4.MoveTowards(m_staminaMeter.color, new Vector4(1, 1, 1, 1), 0.15f);
                    }
                }
                else if (m_inputDelta.normalized.y <= 0 && m_stamina < staminaLevel)
                {
                    m_stamina += staminaDepletionSpeed * Time.deltaTime;
                }
                else if ((!InputManager.Instance.GetKey(m_sprintKey) || Mathf.Abs(m_rigidbody.velocity.x) < 0.01f || Mathf.Abs(m_rigidbody.velocity.z) < 0.01f && m_stamina < staminaLevel))
                {
                    m_stamina += staminaDepletionSpeed * Time.deltaTime;
                }

                if (m_stamina == staminaLevel)
                {
                    m_staminaMeterBG.color = Vector4.MoveTowards(m_staminaMeterBG.color, new Vector4(0, 0, 0, 0), 0.15f);
                    m_staminaMeter.color = Vector4.MoveTowards(m_staminaMeter.color, new Vector4(1, 1, 1, 0), 0.15f);
                }

                float x = Mathf.Clamp(Mathf.SmoothDamp(m_staminaMeter.transform.localScale.x, (m_stamina / staminaLevel) * m_staminaMeterBG.transform.localScale.x, ref m_staminaSmoothRef, (1) * Time.deltaTime, 1), 0.001f, m_staminaMeterBG.transform.localScale.x);
                m_staminaMeter.transform.localScale = new Vector3(x, 1, 1);
                m_stamina = Mathf.Clamp(m_stamina, 0, staminaLevel);
            }
            else
            {
                m_isSprinting = InputManager.Instance.GetKey(m_sprintKey);
            }
        }

        private void HandleCamera(float mouseXInput, float mouseYInput, bool isTurning, float camFOV)
        {
            //ensure all target angles are clamped between 0-360
            if (m_targetAngles.y > 180)
            {
                m_targetAngles.y -= 360;
                m_followAngles.y -= 360;
            }
            else if (m_targetAngles.y < -180)
            {
                m_targetAngles.y += 360;
                m_followAngles.y += 360;
            }

            if (m_targetAngles.x > 180)
            {
                m_targetAngles.x -= 360;
                m_followAngles.x -= 360;
            }
            else if (m_targetAngles.x < -180)
            {
                m_targetAngles.x += 360;
                m_followAngles.x += 360;
            }

            m_targetAngles.y += mouseXInput * (mouseSensitivity / 2 - ((m_CamFOV - camFOV) * fOVToMouseSensitivity) / 6f);
            m_targetAngles.x += mouseYInput * (mouseSensitivity / (isTurning ? 4 : 2) - ((m_CamFOV - camFOV) * fOVToMouseSensitivity) / 6f);
            m_targetAngles.x = Mathf.Clamp(m_targetAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);

            m_followAngles = Vector3.SmoothDamp(m_followAngles, m_targetAngles, ref m_followVelocity, (cameraSmoothing) / 100);

            playerCamera.transform.localRotation = Quaternion.Euler(-m_followAngles.x + m_originalRotation.x, 0, 0);
            transform.localRotation = Quaternion.Euler(0, m_followAngles.y + m_originalRotation.y, 0);
        }

        private void HandleFootsteps(bool ismoving)
        {
            if (IsGrounded)
            {
                if(ismoving)
                {
                    float strideLangthen = 0;
                    float flatVel = 0;

                    Vector3 vel = (m_rigidbody.position - m_lastPosition) / Time.deltaTime;
                    Vector3 velChange = vel - m_previousVelocity;
                    m_previousPosition = m_rigidbody.position;
                    m_previousVelocity = vel;

                    flatVel = new Vector3(vel.x, 0.0f, vel.z).magnitude;
                    strideLangthen = 1 + (flatVel * ((footstepFrequency * 2) / 10));
                    m_footstepCycle += (flatVel / strideLangthen) * (Time.deltaTime / footstepFrequency);

                    if (m_footstepCycle > m_footstep)
                    {
                        m_footstep = m_footstepCycle + 0.5f;
                        m_audioSource.PlayOneShot(footstepClip, footstepVolume);
                    }
                }
            }
        }



        private void HandleAnimation()
        {
            //MovementID
            //idle = -1
            //Walk = 0
            //run = 1
            //strife left = 2
            //strife right = 3
            //turn left = 4
            //turn right = 5

            if (animator != null && !OverrideAnimations)
            {
                if (ChairManager.Instance.HasPlayerOccupiedChair(ID))
                {
                    if (!string.IsNullOrEmpty(SittingAnimation))
                    {
                        m_movement = -1;
                        animator.SetBool("Turning", false);
                        animator.SetBool("Strifing", false);
                        animator.SetBool("Moved", false);

                        animator.SetBool(SittingAnimation, true);
                    }
                    return;
                }
                else if (VehicleManager.Instance.HasPlayerEntertedVehcile(ID))
                {
                    m_movement = -1;
                    animator.SetBool("Turning", false);
                    animator.SetBool("Strifing", false);
                    animator.SetBool("Moved", false);

                    animator.SetBool(VehicleManager.Instance.DrivingAnimation, true);
                    return;
                }
                else
                {
                    if (!string.IsNullOrEmpty(SittingAnimation))
                    {
                        animator.SetBool(SittingAnimation, false);
                    }
                }

                if (!FreezePosition)
                {
                    m_frameCount++;

                    if(m_isInWater != IsInWater)
                    {
                        if(waterSoundEffect != null)
                        {
                            if (IsInWater)
                            {
                                if (!waterSoundEffect.isPlaying)
                                {
                                    PlayWaterSoundEffect(swimmingSound, true);
                                    m_isInWater = IsInWater;
                                }
                            }
                            else
                            {
                                PlayWaterSoundEffect(null, true);
                                m_isInWater = IsInWater;
                            }
                        }
                    }

                    if (m_frameCount > m_frameLimit)
                    {
                        m_frameCount = 0;

                        if (!InputManager.Instance.AnyKeyHeldDown() && !InputManager.Instance.AnyMouseButtonDown())
                        {
                            m_lastPosition = transform.position;
                            m_lastRotation = transform.rotation;
                        }

                        if (m_lastPosition != transform.position || m_lastRotation != transform.rotation)
                        {
                            bool ignoregeneralMovement = false;

                            if (IsInWater)
                            {
                                //cast ray down to see if hit floor
                                //show change this to out of water
                                bool isBelowWater = (WaterHandler != null) ? WaterHandler.IsBelowWater : Physics.Raycast(transform.position, Vector3.down, 1);

                                if (!isBelowWater)
                                {
                                    animator.SetBool("Swimming", false);
                                }
                                else
                                {
                                    animator.SetBool("Emote", false);
                                    animator.SetBool("Moved", false);
                                    animator.SetBool("Strifing", false);
                                    animator.SetBool("Moved", false);

                                    ignoregeneralMovement = true;

                                    animator.SetBool("Swimming", true);
                                    animator.SetFloat("SwimVal", 0.01f);
                                    m_movement = 11;
                                }
                            }

                            MovementID = m_movement;

                            if (!ignoregeneralMovement)
                            {
                                animator.SetBool("Swimming", false);

                                if (MovementID.Equals(-1))
                                {
                                    animator.SetBool("Turning", false);
                                    animator.SetBool("Strifing", false);
                                    animator.SetBool("Moved", false);
                                }
                                else if (MovementID.Equals(0) || MovementID.Equals(1))
                                {
                                    animator.SetBool("Emote", false);
                                    animator.SetBool("Turning", false);
                                    animator.SetBool("Strifing", false);
                                    animator.SetBool("Moved", true);

                                    if (MovementID.Equals(1) && m_moveVal <= 0.01f)
                                    {
                                        m_moveVal += Time.deltaTime * (walkSpeed * 2);
                                    }
                                    else if (m_moveVal >= 0f)
                                    {
                                        m_moveVal -= Time.deltaTime * (walkSpeed * 2);
                                    }

                                    m_moveVal = Mathf.Clamp(m_moveVal, 0.0f, 0.01f);

                                    animator.SetFloat("MovedVal", m_moveVal, 1f, Time.deltaTime * (walkSpeed * 2));
                                }
                                else if (MovementID.Equals(2) || MovementID.Equals(3))
                                {
                                    animator.SetBool("Emote", false);
                                    animator.SetBool("Turning", false);
                                    animator.SetBool("Moved", false);
                                    animator.SetBool("Strifing", true);

                                    if (MovementID.Equals(3) && m_strifeVal <= 0.01f)
                                    {
                                        m_strifeVal += Time.deltaTime * walkSpeed;
                                    }
                                    else if (m_strifeVal >= -0.01f)
                                    {
                                        m_strifeVal -= Time.deltaTime * walkSpeed;
                                    }

                                    m_strifeVal = Mathf.Clamp(m_strifeVal, -0.01f, 0.01f);

                                    animator.SetFloat("StrifeVal", m_strifeVal, 0.5f, Time.deltaTime * walkSpeed);
                                }
                                else if (MovementID.Equals(4) || MovementID.Equals(5))
                                {
                                    animator.SetBool("Emote", false);
                                    animator.SetBool("Moved", false);
                                    animator.SetBool("Strifing", false);
                                    animator.SetBool("Turning", true);

                                    if (MovementID.Equals(5) && m_turnVal <= 0.01f)
                                    {
                                        m_turnVal += Time.deltaTime * walkSpeed;
                                    }
                                    else if (m_strifeVal >= -0.01f)
                                    {
                                        m_turnVal -= Time.deltaTime * walkSpeed;
                                    }

                                    m_turnVal = Mathf.Clamp(m_turnVal, -0.01f, 0.01f);

                                    animator.SetFloat("TurnVal", m_strifeVal, 0.5f, Time.deltaTime * walkSpeed);
                                }
                            }
                        }
                        else
                        {
                            bool ignoregeneralMovement = false;

                            if (IsInWater)
                            {
                                //cast ray down to see if hit floor
                                bool isBelowWater = (WaterHandler != null) ? WaterHandler.IsBelowWater : Physics.Raycast(transform.position, Vector3.down, 1);

                                if (!isBelowWater)
                                {
                                    animator.SetBool("Swimming", false);
                                }
                                else
                                {
                                    ignoregeneralMovement = true;

                                    animator.SetBool("Swimming", true);
                                    animator.SetBool("Turning", false);
                                    animator.SetBool("Strifing", false);
                                    animator.SetBool("Moved", false);

                                    animator.SetFloat("SwimVal", -0.01f);

                                    MovementID = 10;
                                    m_movement = MovementID;
                                }
                            }

                            if (!ignoregeneralMovement)
                            {
                                animator.SetBool("Turning", false);
                                animator.SetBool("Strifing", false);
                                animator.SetBool("Moved", false);
                                animator.SetBool("Swimming", false);
                                MovementID = -1;
                                m_movement = MovementID;
                            }
                        }

                        m_lastPosition = transform.position;
                        m_lastRotation = transform.rotation;
                        m_frameCount = 0;
                    }
                }
                else
                {
                    m_lastPosition = transform.position;
                    bool ignoregeneralMovement = false;

                    if(IsInWater)
                    {
                        bool isBelowWater = (WaterHandler != null) ? WaterHandler.IsBelowWater : Physics.Raycast(transform.position, Vector3.down, 1);

                        if (!isBelowWater)
                        {
                            animator.SetBool("Swimming", false);
                        }
                        else
                        {
                            animator.SetBool("Swimming", true);
                            animator.SetBool("Turning", false);
                            animator.SetBool("Strifing", false);
                            animator.SetBool("Moved", false);
                            ignoregeneralMovement = true;

                            animator.SetFloat("SwimVal", -0.01f);

                            MovementID = 10;
                            m_movement = MovementID;
                        }
                    }

                    if (!ignoregeneralMovement)
                    {
                        if (animator.GetBool("Moved") && !OverrideAnimationHandler)
                        {
                            animator.SetBool("Swimming", false);
                            animator.SetBool("Moved", false);
                            animator.SetBool("Strifing", false);
                            animator.SetBool("Turning", false);
                            MovementID = -1;
                            m_movement = MovementID;

                            animator.SetBool("Running", m_isSprinting);
                        }
                    }

                    m_lastPosition = transform.position;
                    m_lastRotation = transform.rotation;
                    m_frameCount = 0;
                }
            }
        }

        public void PlayWaterSoundEffect(AudioClip clip, bool loop = false)
        {
            if(waterSoundEffect != null)
            {
                if(waterSoundEffect.isPlaying)
                {
                    waterSoundEffect.Stop();
                }

                if(clip != null)
                {
                    if (loop)
                    {
                        waterSoundEffect.loop = true;
                        waterSoundEffect.clip = clip;
                        waterSoundEffect.Play();
                    }
                    else
                    {
                        waterSoundEffect.loop = false;
                        waterSoundEffect.PlayOneShot(clip);
                    }
                }
            }
        }


        public void ToggleFocus(bool focus)
        {
            if (m_focusProcess != null)
            {
                StopCoroutine(m_focusProcess);
            }

            m_focusProcess = StartCoroutine(Focus(focus));
        }

        public void ToggleVisibility(bool isVisible)
        {
            string layer = (isVisible) ? (m_isMine) ? "Avatar" : "Default" : "TransparentFX";

            Avatar.layer = LayerMask.NameToLayer(layer);

            foreach (var child in Avatar.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = LayerMask.NameToLayer(layer);
            }

            GetComponent<MMOPlayer>().LabelNameFront.transform.parent.gameObject.SetActive(isVisible);
        }

        private IEnumerator Focus(bool focus, bool jump = false)
        {
            if (!jump)
            {
                if (focus)
                {
                    m_wasInThirdPersonFocus = PlayerManager.Instance.ThirdPersonCameraActive;

                    if (m_wasInThirdPersonFocus)
                    {
                        PlayerManager.Instance.cameraMenuHandler.SelectFP();
                    }
                }
            }

            m_isFocused = focus;

            float to = 0;
            float from = m_CamFOV;

            if (focus)
            {
                to = m_maxFocus;
            }
            else
            {
                to = m_cacheFOV;
            }

            if (!jump)
            {
                float percentage = 0.0f;
                float runningtimer = 0.0f;

                while (percentage < 1.0f)
                {
                    runningtimer += Time.deltaTime;
                    percentage = runningtimer / 0.5f;

                    m_CamFOV = Mathf.Lerp(from, to, percentage);

                    yield return null;
                }

                if (!focus)
                {
                    if (m_wasInThirdPersonFocus)
                    {
                        PlayerManager.Instance.cameraMenuHandler.SelectTP();
                    }

                    m_wasInThirdPersonFocus = false;
                }
            }

            m_CamFOV = to;
        }

        public void SwitchToThirdPerson(bool thirdPerson)
        {
            m_thirdPerson = thirdPerson;

            if (thirdPerson)
            {
                playerCamera.gameObject.SetActive(false);
                thirdPersonCamera.gameObject.SetActive(true);
            }
            else
            {
                playerCamera.gameObject.SetActive(true);
                thirdPersonCamera.gameObject.SetActive(false);

                //need to reset the camera rotation
                MainCamera.transform.eulerAngles = new Vector3(0, PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.eulerAngles.y, 0);
                TargetCameraRotation = PlayerManager.Instance.GetLocalPlayer().MainCamera.transform.eulerAngles;
            }
        }

        public void EnableCameras(bool enable)
        {
            if (!enable)
            {
                playerCamera.gameObject.SetActive(false);
                thirdPersonCamera.gameObject.SetActive(false);
            }
            else
            {
                SwitchToThirdPerson(m_thirdPerson);
            }
        }

        /// <summary>
        /// Rotates the players avatar based on walking direction
        /// </summary>
        /// <param name="forawrd"></param>
        /// <param name="backward"></param>
        public void RotateAvatar(bool forawrd, bool backward)
        {
            if (m_isMine)
            {
                if (CoreManager.Instance.playerSettings.avatarBackwardsBehaviour.Equals(AvatarBackwardBehaviour.Rotate))
                {
                    if (forawrd)
                    {
                        animatorHolder.localEulerAngles = Vector3.zero;
                    }
                    else if (backward)
                    {
                        animatorHolder.localEulerAngles = new Vector3(0, 180, 0);
                    }
                    else
                    {
                        animatorHolder.localEulerAngles = Vector3.zero;
                    }
                }
            }
        }

        /// <summary>
        /// Changes the avatar based on sex and settings
        /// </summary>
        /// <param name="sex"></param>
        /// <param name="settings"></param>
        public void UpdateAvatar(CustomiseAvatar.Sex sex, AvatarCustomiseSettings settings)
        {
            if (animator != null)
            {
                Destroy(animator.gameObject);
            }

            if (CoreManager.Instance.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.Standard))
            {
                if (sex.Equals(CustomiseAvatar.Sex.Male))
                {
                    UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.standardMan);
                    GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, animatorHolder);
                    go.name = AppManager.Instance.Settings.playerSettings.standardMan;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = AppManager.Instance.Settings.playerSettings.scaleStandardMan;
                    go.transform.localEulerAngles = Vector3.zero;

                    string layer = (m_isMine) ? "Avatar" : "Default";

                    go.layer = LayerMask.NameToLayer(layer);

                    foreach (var child in go.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.layer = LayerMask.NameToLayer(layer);
                    }

                    go.SetActive(true);

                    man = go;
                    animator = man.GetComponent<Animator>();
                    man.GetComponent<ICustomAvatar>().Customise(settings);

                    if (OnAvatarCreated != null)
                    {
                        OnAvatarCreated.Invoke(go);
                    }
                }
                else
                {
                    UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.standardWoman);
                    GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, animatorHolder);
                    go.name = AppManager.Instance.Settings.playerSettings.standardWoman;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = AppManager.Instance.Settings.playerSettings.scaleStandardWoman;
                    go.transform.localEulerAngles = Vector3.zero;

                    string layer = (m_isMine) ? "Avatar" : "Default";

                    go.layer = LayerMask.NameToLayer(layer);

                    foreach (var child in go.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.layer = LayerMask.NameToLayer(layer);
                    }

                    go.SetActive(true);

                    woman = go;
                    animator = woman.GetComponent<Animator>();
                    woman.GetComponent<ICustomAvatar>().Customise(settings);

                    if (OnAvatarCreated != null)
                    {
                        OnAvatarCreated.Invoke(go);
                    }
                }
            }
            else
            {
                if (sex.Equals(CustomiseAvatar.Sex.Male))
                {
                    UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.simpleMan);
                    GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, animatorHolder);
                    go.name = CoreManager.Instance.playerSettings.simpleMan;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = CoreManager.Instance.playerSettings.scaleSimpleMan;
                    go.transform.localEulerAngles = Vector3.zero;

                    string layer = (m_isMine) ? "Avatar" : "Default";

                    go.layer = LayerMask.NameToLayer(layer);

                    foreach (var child in go.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.layer = LayerMask.NameToLayer(layer);
                    }

                    go.SetActive(true);

                    man = go;
                    animator = man.GetComponent<Animator>();
                    man.GetComponent<ICustomAvatar>().Customise(settings);

                    if (OnAvatarCreated != null)
                    {
                        OnAvatarCreated.Invoke(go);
                    }
                }
                else
                {
                    UnityEngine.Object prefab = Resources.Load(CoreManager.Instance.playerSettings.simpleWoman);
                    GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, animatorHolder);
                    go.name = CoreManager.Instance.playerSettings.simpleWoman;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = CoreManager.Instance.playerSettings.scaleSimpleWoman;
                    go.transform.localEulerAngles = Vector3.zero;

                    string layer = (m_isMine) ? "Avatar" : "Default";

                    go.layer = LayerMask.NameToLayer(layer);

                    foreach (var child in go.GetComponentsInChildren<Transform>(true))
                    {
                        child.gameObject.layer = LayerMask.NameToLayer(layer);
                    }

                    go.SetActive(true);

                    woman = go;
                    animator = woman.GetComponent<Animator>();
                    woman.GetComponent<ICustomAvatar>().Customise(settings);

                    if (OnAvatarCreated != null)
                    {
                        OnAvatarCreated.Invoke(go);
                    }
                }
            }

            MMOPlayer nPlayer = GetComponent<MMOPlayer>();

            if (nPlayer)
            {
                nPlayer.animator = animator;
                nPlayer.animatorHolder = animatorHolder;
            }

            if (IsLocal)
            {
                animatorHolder.transform.localScale = Vector3.one;
            }
        }

        public void UpdateAvatar(string customAvatar)
        {
            if (animator != null)
            {
                Destroy(animator.gameObject);
            }

            customAvatars.Clear();

            if (AppManager.Instance.Settings.projectSettings.avatarSetupMode.Equals(AvatarSetupMode.ReadyPlayerMe) &&
                AppManager.Instance.Settings.projectSettings.readyPlayerMeMode.Equals(ReadyPlayerMeMode.Selfie))
            {
#if BRANDLAB360_AVATARS_READYPLAYERME
                RPMPlayer rpmPlayer = GetComponent<RPMPlayer>();

                if (rpmPlayer == null)
                {
                    rpmPlayer = gameObject.AddComponent<RPMPlayer>();
                }

                UnityEngine.Object prefab = Resources.Load(customAvatar);

                if (prefab == null)
                {
                    rpmPlayer.LoadAvatar(customAvatar, RPMLoadComplete);
                }
                else
                {
                    GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, animatorHolder);
                    RPMLoadComplete(go);
                }
#endif
            }
            else
            {
                for (int i = 0; i < CoreManager.Instance.playerSettings.fixedAvatars.Count; i++)
                {
                    if (CoreManager.Instance.playerSettings.fixedAvatars[i].Equals(customAvatar))
                    {
                        UnityEngine.Object prefab = Resources.Load(AppManager.Instance.Settings.playerSettings.fixedAvatars[i]);
                        GameObject go = (GameObject)Instantiate(prefab, Vector3.zero, Quaternion.identity, animatorHolder);
                        go.name = CoreManager.Instance.playerSettings.fixedAvatars[i];
                        go.transform.localPosition = Vector3.zero;
                        go.transform.localEulerAngles = Vector3.zero;
                        go.transform.localScale = AppManager.Instance.Settings.playerSettings.fixedAvatarScales[i];
                        go.SetActive(true);

                        string layer = (m_isMine) ? "Avatar" : "Default";

                        go.layer = LayerMask.NameToLayer(layer);

                        foreach (var child in go.GetComponentsInChildren<Transform>(true))
                        {
                            child.gameObject.layer = LayerMask.NameToLayer(layer);
                        }

                        customAvatars.Add(go);
                        animator = go.GetComponent<Animator>();

                        MMOPlayer nPlayer = GetComponent<MMOPlayer>();

                        if (nPlayer)
                        {
                            nPlayer.animator = animator;
                            nPlayer.animatorHolder = animatorHolder;
                        }

                        if (OnAvatarCreated != null)
                        {
                            OnAvatarCreated.Invoke(go);
                        }

                        break;
                    }
                }

                animatorHolder.transform.localScale = Vector3.one;
            }

        }

        private void RPMLoadComplete(GameObject go)
        {
            go.transform.SetParent(animatorHolder);
            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localScale = AppManager.Instance.Settings.playerSettings.RPMAvatarScale;
            go.SetActive(true);

            string layer = (m_isMine) ? "Avatar" : "Default";

            go.layer = LayerMask.NameToLayer(layer);

            foreach (var child in go.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = LayerMask.NameToLayer(layer);
            }

            animator = go.GetComponent<Animator>();

            MMOPlayer nPlayer = GetComponent<MMOPlayer>();

            if (nPlayer)
            {
                nPlayer.animator = animator;
                nPlayer.animatorHolder = animatorHolder;
            }

            if (OnAvatarCreated != null)
            {
                OnAvatarCreated.Invoke(go);
            }

            animatorHolder.transform.localScale = Vector3.one;
        }

        public void ApplySettings()
        {
            //fixed
            maxStepHeight = CoreManager.Instance.playerSettings.maxStepHeight;
            maxSlopeAngle = CoreManager.Instance.playerSettings.maxSlopeAngle;

            jumpEnabled = CoreManager.Instance.playerSettings.jumpEnabled;
            jumpPower = CoreManager.Instance.playerSettings.jumpPower;
            m_jumpPower = jumpPower;

            footstepVolume = CoreManager.Instance.playerSettings.footstepVolume;


            //player controlled
            mouseSensitivity = PlayerManager.Instance.MainControlSettings.mouse;

            invertMouseX = PlayerManager.Instance.MainControlSettings.invertX > 0 ? true : false;
            invertMouseY = PlayerManager.Instance.MainControlSettings.invertY > 0 ? true : false;

            walkSpeed = PlayerManager.Instance.MainControlSettings.walk;
            m_walkSpeed = walkSpeed;

            sprintSpeed = PlayerManager.Instance.MainControlSettings.run;
            m_sprintSpeed = sprintSpeed;
            m_strafingSpeed = PlayerManager.Instance.MainControlSettings.strife;

            m_mouseButtonClick = (CoreManager.Instance.playerSettings.inputButton.Equals(PlayerControlSettings.MouseInputButton._0)) ? 0 : 1;

            //set up controls
            for (int i = 0; i < PlayerManager.Instance.MainControlSettings.controls.Count; i++)
            {
                //forward
                if (i == 0)
                {
                    m_forwardKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //back
                if (i == 1)
                {
                    m_backKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //left
                if (i == 2)
                {
                    m_leftKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //right
                if (i == 3)
                {
                    m_rightKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //sprint
                if (i == 4)
                {
                    m_sprintKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //strife left
                if (i == 5)
                {
                    m_strifeLeftKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //strife right
                if (i == 6)
                {
                    m_strifeRightKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //focus
                if (i == 7)
                {
                    m_focusKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }

                //interaction
                if (i == 8)
                {
                    m_interactionKey = InputManager.Instance.GetKeyName(((InputKeyCode)PlayerManager.Instance.MainControlSettings.controls[i]).ToString());
                }
            }
        }

        public void Move(Vector3 vec)
        {
            if (m_isMine)
            {
                if (!FreezePosition)
                {
                    bool isTurning = false;

                    float z = Mathf.Clamp(vec.z, -1.0f, 1.0f);
                    float x = Mathf.Clamp(vec.x, -1.0f, 1.0f);
                    m_inputDelta = new Vector2(x / 10, z);

                    if (z > 0.05f || z < -0.05f)
                    {
                       if (z >= 1.0f && AppManager.Instance.Settings.playerSettings.canRunInJoystick)
                        {
                            m_movement = 1;
                        }
                        else
                        {
                            m_movement = 0;
                        }

                        if (z < 0.0f)
                        {
                            m_inputDelta = new Vector2(m_inputDelta.x / 10, -0.5f);
                        }

                        isTurning = false;
                        m_turnVal = 0.0f;
                    }
                    else
                    {
                        m_moveVal = 0.0f;

                        if (vec.x < 0.0f)
                        {
                            isTurning = true;
                            m_movement = 4;
                        }
                        else if (vec.x > 0.0f)
                        {
                            isTurning = true;
                            m_movement = 5;
                        }
                        else
                        {
                            isTurning = false;
                            m_turnVal = 0.0f;
                            m_movement = -1;
                        }
                    }

                    if(AppManager.Instance.Settings.playerSettings.mobileControllerType.Equals(PlayerControlSettings.MobileControlType._Joystick))
                    {
                        RotatePlayer((invertMouseX ? vec.x * -1 : vec.x), isTurning);
                    }
                    else
                    {
                        if(m_movement.Equals(4) || m_movement.Equals(5))
                        {
                            RotatePlayer((invertMouseX ? vec.x * -1 : vec.x), isTurning);
                        }
                        else
                        {
                            if(!isTurning)
                            {
                                RotatePlayer(vec.x, isTurning);
                            }
                        }
                    }
                }
                else
                {
                    m_turnVal = 0.0f;
                    m_moveVal = 0.0f;
                    MovementID = -1;
                }
            }
        }

        private void RotatePlayer(float x, bool isTurning)
        {
            float camFOV = m_playerCamera.Lens.FieldOfView;

            if (m_targetAngles.y > 180) 
            {
                m_targetAngles.y -= 360;
                m_followAngles.y -= 360; 
            } 
            else if (m_targetAngles.y < -180) 
            {
                m_targetAngles.y += 360;
                m_followAngles.y += 360; 
            }

            //ensure that joystick control is slower
            float sensitivity = AppManager.Instance.Settings.playerSettings.mobileControllerType.Equals(PlayerControlSettings.MobileControlType._Arrows) ? AppManager.Instance.Settings.playerSettings.sensitivity : AppManager.Instance.Settings.playerSettings.sensitivity / 10;

            m_targetAngles.y += x / (isTurning ? 4 : 2) * (sensitivity - ((m_CamFOV - camFOV) * fOVToMouseSensitivity) / 6f);
            m_followAngles = Vector3.SmoothDamp(m_followAngles, m_targetAngles, ref m_followVelocity, (cameraSmoothing) / 100);

            playerCamera.transform.localRotation = Quaternion.Euler(-m_followAngles.x + m_originalRotation.x, 0, 0);
            transform.localRotation = Quaternion.Euler(0, m_followAngles.y + m_originalRotation.y, 0);
        }

        public void ToggleCanMove(bool canMove)
        {
            m_canMove = canMove;
        }

        public void ToggleTopDown(bool isOn)
        {
            m_topDownActive = isOn;

            if (PlayerManager.Instance.ThirdPersonCameraActive)
            {
                thirdPersonCamera.gameObject.SetActive(!isOn);
            }
            else
            {
                MainCamera.gameObject.SetActive(!isOn);
            }

            if(m_topDownActive)
            {

            }
        }

        private void OnCollisionEnter(Collision CollisionData)
        {
            for (int i = 0; i < CollisionData.contactCount; i++)
            {
                float a = Vector3.Angle(CollisionData.GetContact(i).normal, Vector3.up);

                if (CollisionData.GetContact(i).point.y < transform.position.y - ((m_capsule.height / 2) - m_capsule.radius * 0.95f))
                {
                    if (!IsGrounded)
                    {
                        IsGrounded = true;
                    }

                }
            }
        }
        private void OnCollisionStay(Collision CollisionData)
        {
            for (int i = 0; i < CollisionData.contactCount; i++)
            {
                float a = Vector3.Angle(CollisionData.GetContact(i).normal, Vector3.up);

                if (CollisionData.GetContact(i).point.y < transform.position.y - ((m_capsule.height / 2) - m_capsule.radius * 0.95f))
                {
                    if (!IsGrounded)
                    {
                        IsGrounded = true;
                    }

                }
            }
        }
        private void OnCollisionExit(Collision CollisionData)
        {
            IsGrounded = false;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PlayerController), true)]
        public class PlayerController_Editor : BaseInspectorEditor
        {
            private void OnEnable()
            {
                GetBanner();
            }

            public override void OnInspectorGUI()
            {
                DisplayBanner();

                if (Application.productName.Equals("BL360 Plugin"))
                {
                    serializedObject.Update();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("playerCamera"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("thirdPersonCamera"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ProductHolder"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pointer"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("waterSoundEffect"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("swimmingSound"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animator"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorHolder"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("man"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("woman"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("simpleMan"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("simpleWoman"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customAvatars"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("walkSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sprintSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpEnabled"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpPower"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSlopeAngle"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStepHeight"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("stepSmooth"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("footstepClip"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("footstepVolume"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("footstepFrequency"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaDepletionSpeed"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("staminaLevel"), true);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalRotationRange"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("mouseSensitivity"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fOVToMouseSensitivity"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraSmoothing"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("invertMouseX"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("invertMouseY"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("FOVKickAmount"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("changeTime"), true);

                    if (GUI.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(this);
                    }
                }
            }
        }
#endif
    }
}
