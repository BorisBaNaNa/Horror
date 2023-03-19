//#define USE_CINEMACHINE
#if USE_CINEMACHINE
using Cinemachine;
#endif

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public bool IsFalling => _verticalVelocity.y < 0;
    public bool IsFlaying => _verticalVelocity.y > 0;
    public bool IsGrounded => _characterController.isGrounded;

    [Header("Camera setup")]
#if USE_CINEMACHINE
    public CinemachineVirtualCamera MainCamera;
#else
    public Camera MainCamera;
#endif
    public bool EnableCameraRotation = true;
    public bool InvertCameraXRotation = false;
    public bool InvertCameraYRotation = false;
    public Vector2 MouseSencitivity = new(0.5f, 0.5f);
    [Range(30f, 120f)]
    public float FieldOfView = 60f;

    [SerializeField]
    private Vector3 CameraPos;
    [SerializeField, Range(-90f, 0f)]
    private float MinY = -85f;
    [SerializeField, Range(0f, 90f)]
    private float MaxY = 85f;

    [Header("SwayCamera")]
    public bool EnableSwayCamera = true;

#if USE_CINEMACHINE
    [SerializeField]
    private float IdleAmplitude = 1f;
    [SerializeField]
    private float IdleFrequency = 0.01f;
    [SerializeField]
    private float WalkAmplitude = 2f;
    [SerializeField]
    private float WalkFrequency = 0.03f;
    [SerializeField]
    private float CrouchAmplitude = 0.5f;
    [SerializeField]
    private float CrouchFrequency = 0.01f;
    [SerializeField]
    private float SprintAmplitude = 2f;
    [SerializeField]
    private float SprintFrequency = 0.07f;

    private CinemachineBasicMultiChannelPerlin _channelPerlin;
#else
    [SerializeField]
    private float SwayIntensity = 1.5f;
    [SerializeField]
    private float Amplitude = 0.25f;
#endif
    [Header("Movement")]
    public bool EnableMoovement = true;

    [SerializeField]
    private float WalkSpeed = 10;
    [SerializeField]
    private float AccelerationTimeGrounded = 0.1f;

    [Header("Sprint")]
    public bool EnableSprint = true;
    public bool HoldToSprint = true;
    public bool UnlimitedSprint = false;

    [SerializeField]
    private float SprintSpeed;
    [SerializeField]
    private float SprintDuration;
    [SerializeField]
    private float SprintCooldown;
    [SerializeField, Range(1f, 3f)]
    private float FatigueRatio = 1f;
    [SerializeField, Range(1f, 3f)]
    private float RelaxRatio = 1f;

    [Header("Jump")]
    public bool EnableJump = true;

    [SerializeField]
    private float JumpPeriod = 1f;
    [SerializeField]
    private float AccelerationTimeAirbone = 0.3f;
    [SerializeField]
    private LayerMask GroundLayerMask;

    [Header("Crouch")]
    public bool EnableCrouch = true;
    public bool HoldToCrouch = true;

    [SerializeField, Range(0.1f, 1f)]
    private float CrouchHeightRatio = 0.6f;
    [SerializeField]
    private float CrouchSpeed;
    [SerializeField, Range(1f, 10f)]
    private float SpeedReduction = 1f;


    private Action<InputAction.CallbackContext> _sprintAction;
    private Action<InputAction.CallbackContext> _startSprintAction;
    private Action<InputAction.CallbackContext> _finishSprintAction;
    private Action<InputAction.CallbackContext> _crouchAction;
    private Action<InputAction.CallbackContext> _startCrouchAction;
    private Action<InputAction.CallbackContext> _finishCrouchAction;

    private InputControls _input;
    private CharacterController _characterController;
    private Coroutine _crouchCoroutine;
    private Vector3 _inputVector;
    private Vector3 _verticalVelocity;
    private Vector3 _horizontalVelocity;
    private Vector3 _velocitySmoothing;
    private Vector3 _targetCameraPos;
    private Vector2 _mouseDelta;
    private float _jumpVelocity;
    private float _xRotation;
    private float _currentSpeed;
    private float _nextSprintTime;
    private float _currentSprintDuration;
    private float _heightOriginal;
    private float _targetHeight;
    private bool _isSprinting;
    private bool _isCrouching;
    private bool _lastHoldToSprint;
    private bool _lastHoldToCrouch;

#if USE_CINEMACHINE

#else
    private Vector3 _nextSwayVector;
    private Vector3 _nextSwayPosition;
    private float _currentSwayIntensity;
#endif

    private void Awake()
    {
        Initialize();
        InitializeActions();
        InitializeInput();
    }

    private void Update()
    {
        ApplyCameraSettings();
        SwitchOnHoldOrPress();
        CheckAndTurnInputActions();
        SprintDurationControl();
        LerpCrouching();
        CameraSway();

        ReadInput();

        Rotate(_mouseDelta);

        ApplySmooth();
        MovePlayer(_horizontalVelocity);

        ApplyGravity();
        MovePlayer(_verticalVelocity);
        CheckHeadbutt();

#if USE_CINEMACHINE
        StateCheck();
#endif
    }

    private void OnEnable()
    {
        _input.Player.Enable();
    }

    private void OnDisable()
    {
        _input.Player.Disable();
    }

    private void Initialize()
    {
        _input = new InputControls();
        _characterController = GetComponent<CharacterController>();

#if USE_CINEMACHINE
        InitializeCinemachineCamera();
#else
        InitializeUnityCamera();
#endif
        _jumpVelocity = JumpPeriod * Mathf.Abs(Physics.gravity.y);
        _currentSpeed = WalkSpeed;
        _currentSprintDuration = SprintDuration;
        _targetHeight = _heightOriginal = _characterController.height;
        _targetCameraPos = CameraPos;

    }

    private void InitializeActions()
    {
        _sprintAction = _ => Sprint();
        _startSprintAction = _ => StartSprint();
        _finishSprintAction = _ => FinishSprint();

        _crouchAction = _ => Crouch();
        _startCrouchAction = _ => StartCrouch();
        _finishCrouchAction = _ => StartfinishCrouchCoroutine();
    }

    private void InitializeInput()
    {
        _input.Player.Jump.performed += _ => Jump();
        InitializeSprintButton(HoldToSprint);
        InitializeCrouchButton(HoldToCrouch);
    }

    private void InitializeSprintButton(bool isHolding)
    {
        if (isHolding)
        {
            _input.Player.Sprint.performed -= _sprintAction;
            _input.Player.Sprint.started += _startSprintAction;
            _input.Player.Sprint.canceled += _finishSprintAction;
        }
        else
        {
            _input.Player.Sprint.started -= _startSprintAction;
            _input.Player.Sprint.canceled -= _finishSprintAction;
            _input.Player.Sprint.performed += _sprintAction;
        }
        _lastHoldToSprint = isHolding;
    }

    private void InitializeCrouchButton(bool isCrouching)
    {
        if (isCrouching)
        {
            _input.Player.Crouch.performed -= _crouchAction;
            _input.Player.Crouch.started += _startCrouchAction;
            _input.Player.Crouch.canceled += _finishCrouchAction;
        }
        else
        {
            _input.Player.Crouch.started -= _startCrouchAction;
            _input.Player.Crouch.canceled -= _finishCrouchAction;
            _input.Player.Crouch.performed += _crouchAction;
        }
        _lastHoldToCrouch = isCrouching;
    }

#if !USE_CINEMACHINE
    private void InitializeUnityCamera()
    {
        if (MainCamera == null)
            MainCamera = Camera.main;

        MainCamera.transform.parent = transform;
        MainCamera.transform.localPosition = CameraPos;
        MainCamera.transform.localRotation = transform.localRotation;
        MainCamera.fieldOfView = FieldOfView;

        _nextSwayVector = Amplitude * Vector3.up;
        _nextSwayPosition = _targetCameraPos + _nextSwayVector;
        _currentSwayIntensity = SwayIntensity;
    }
#else
    private void InitializeCinemachineCamera()
    {
        if (MainCamera == null)
        {
            var virtualCameras = FindObjectsOfType<CinemachineVirtualCamera>();
            float maxPriority = float.MinValue;
            foreach (var virtualCam in virtualCameras)
            {
                if (virtualCam.Priority > maxPriority)
                {
                    maxPriority = virtualCam.Priority;
                    MainCamera = virtualCam;
                }
            }
        }
        MainCamera.transform.parent = transform;
        MainCamera.transform.localPosition = CameraPos;
        MainCamera.m_Lens.FieldOfView = FieldOfView;

        _channelPerlin = MainCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        //_channelPerlin = MainCamera.
        if (_channelPerlin == null)
            Debug.LogError("_channelPerlin is null!!!");
    }

    private void StateCheck()
    {
        if (_channelPerlin == null) return;

        if (_inputVector != Vector3.zero)
        {
            if (_isSprinting && _channelPerlin.m_AmplitudeGain != SprintAmplitude)
            {
                _channelPerlin.m_AmplitudeGain = SprintAmplitude;
                _channelPerlin.m_FrequencyGain = SprintFrequency;
            }
            else if (_isCrouching && _channelPerlin.m_AmplitudeGain != CrouchAmplitude)
            {
                _channelPerlin.m_AmplitudeGain = CrouchAmplitude;
                _channelPerlin.m_FrequencyGain = CrouchFrequency;
            }
            else if (!_isSprinting && !_isCrouching && _channelPerlin.m_AmplitudeGain != WalkAmplitude)
            {
                _channelPerlin.m_AmplitudeGain = WalkAmplitude;
                _channelPerlin.m_FrequencyGain = WalkFrequency;
            }
        }
        else if (_channelPerlin.m_AmplitudeGain != IdleAmplitude)
        {
            _channelPerlin.m_AmplitudeGain = IdleAmplitude;
            _channelPerlin.m_FrequencyGain = IdleFrequency;
        }
    }
#endif

    public void TeleportTo(Transform transform_)
    {
        _characterController.enabled = false;
        transform.SetPositionAndRotation(transform_.position, transform_.rotation);
        _characterController.enabled = true;
    }

    private void ApplyCameraSettings()
    {
#if USE_CINEMACHINE
        if (MainCamera.m_Lens.FieldOfView != FieldOfView)
            MainCamera.m_Lens.FieldOfView = FieldOfView;
        MainCamera.GetCinemachineComponent<CinemachinePOV>().m_VerticalAxis.m_MaxSpeed = 20 * MouseSencitivity.y;
#else
        if (MainCamera.fieldOfView != FieldOfView)
            MainCamera.fieldOfView = FieldOfView;
#endif
    }

    private void CameraSway()
    {
#if !USE_CINEMACHINE
        if (EnableSwayCamera && _inputVector != Vector3.zero)
        {
            MainCamera.transform.localPosition = Vector3.MoveTowards(MainCamera.transform.localPosition, _nextSwayPosition, _currentSwayIntensity * Time.deltaTime);
            if (Vector3.SqrMagnitude(MainCamera.transform.localPosition - _nextSwayPosition) < 0.01f)
            {
                _nextSwayVector *= -1;
                _nextSwayPosition = _targetCameraPos + _nextSwayVector;
            }
        }
        else
#endif
        MainCamera.transform.localPosition = Vector3.Lerp(MainCamera.transform.localPosition, _targetCameraPos, SpeedReduction * Time.deltaTime);
    }

    private void LerpCrouching()
    {
        if (_characterController.height == _targetHeight && MainCamera.transform.localPosition == _targetCameraPos)
            return;

        _characterController.height = Mathf.MoveTowards(_characterController.height, _targetHeight, SpeedReduction * Time.deltaTime);
    }

    private void SwitchOnHoldOrPress()
    {
        if (_lastHoldToSprint != HoldToSprint)
            InitializeSprintButton(HoldToSprint);
        if (_lastHoldToCrouch != HoldToCrouch)
            InitializeCrouchButton(HoldToCrouch);
    }

    private void ReadInput()
    {
        _inputVector = _input.Player.Move.ReadValue<Vector3>();
        _mouseDelta = _input.Player.CameraRotation.ReadValue<Vector2>();
    }

    private void MovePlayer(Vector3 movement)
    {
        _characterController.Move(transform.rotation * movement * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (IsGrounded && !IsFlaying)
            _verticalVelocity.y = 0f;
        else
            _verticalVelocity += Physics.gravity * 2 * Time.deltaTime;
    }

    private void CheckHeadbutt()
    {
        if (IsFlaying && _characterController.collisionFlags == CollisionFlags.Above)
            _verticalVelocity.y = 0f;
    }

    private void ApplySmooth()
    {
        Vector3 targetVelocity = _inputVector * _currentSpeed;
        float accelerationTime = IsGrounded ? AccelerationTimeGrounded : AccelerationTimeAirbone;
        _horizontalVelocity = Vector3.SmoothDamp(_horizontalVelocity, targetVelocity, ref _velocitySmoothing, accelerationTime);
    }

    private void Jump()
    {
        Vector3 spherePos = transform.position - (Vector3.up * (_characterController.height * 0.25f + 0.1f));
        if (Physics.OverlapSphere(spherePos, _characterController.radius, GroundLayerMask).Length > 0)
            _verticalVelocity.y = _jumpVelocity;
    }

    private void Rotate(Vector2 delta)
    {
        delta *= MouseSencitivity;
        float invertValX = InvertCameraXRotation ? -1 : 1;
        float invertValY = InvertCameraYRotation ? -1 : 1;
#if !USE_CINEMACHINE
        CamRotate(delta.y * invertValY);
#endif
        transform.Rotate(0f, delta.x * invertValX, 0f);
    }

#if !USE_CINEMACHINE
    public void CamRotate(float xAngle)
    {
        _xRotation -= xAngle;
        _xRotation = Mathf.Clamp(_xRotation, MinY, MaxY);
        MainCamera.transform.rotation = Quaternion.Euler(_xRotation, transform.eulerAngles.y, 0);
    }
#endif

    private void CheckAndTurnInputActions()
    {
        TurnIputAction(EnableCameraRotation, _input.Player.CameraRotation);
        TurnIputAction(EnableMoovement, _input.Player.Move);
        TurnIputAction(EnableJump, _input.Player.Jump);
        TurnIputAction(EnableSprint, _input.Player.Sprint);
        TurnIputAction(EnableCrouch, _input.Player.Crouch);
    }

    private void TurnIputAction(bool enableState, InputAction action)
    {
        if (enableState != action.enabled)
        {
            if (enableState)
                action.Enable();
            else
                action.Disable();
        }
    }

    private void Sprint()
    {
        if (!_isSprinting)
            StartSprint();
        else
            FinishSprint();
    }

    private void StartSprint()
    {
        if (_nextSprintTime > Time.time || _isSprinting) return;
        if (_isCrouching)
            FinishCrouch();

        _isSprinting = true;

        _currentSpeed = SprintSpeed;
#if !USE_CINEMACHINE
        _currentSwayIntensity *= 2.5f;
#endif
    }

    private void FinishSprint()
    {
        if (!_isSprinting) return;
        _isSprinting = false;

        _currentSpeed = WalkSpeed;
        _nextSprintTime = Time.time + SprintCooldown;
#if !USE_CINEMACHINE
        _currentSwayIntensity = SwayIntensity;
#endif
    }

    private void SprintDurationControl()
    {
        if (_isSprinting && !UnlimitedSprint)
        {
            if (_currentSprintDuration > 0)
                _currentSprintDuration -= Time.deltaTime * FatigueRatio;
            else
                FinishSprint();
        }
        else if (_currentSprintDuration < SprintDuration)
            _currentSprintDuration += Mathf.Min(Time.deltaTime * RelaxRatio, SprintDuration - _currentSprintDuration);
    }

    private void Crouch()
    {
        if (!_isCrouching)
            StartCrouch();
        else
            FinishCrouch();
    }

    private void StartCrouch()
    {
        if (_crouchCoroutine != null)
        {
            StopCoroutine(_crouchCoroutine);
            _crouchCoroutine = null;
        }
        if (_isCrouching) return;
        if (_isSprinting)
            FinishSprint();

        _isCrouching = true;
        _targetHeight = _characterController.height * CrouchHeightRatio;
        _targetCameraPos = new Vector3(MainCamera.transform.localPosition.x,
                                                MainCamera.transform.localPosition.y * CrouchHeightRatio,
                                                MainCamera.transform.localPosition.z);
        _currentSpeed = CrouchSpeed;

#if USE_CINEMACHINE

#else
        _currentSwayIntensity *= 0.5f;
#endif
    }

    private bool FinishCrouch()
    {
        if (Physics.Raycast(transform.position - (Vector3.up * (_characterController.height * 0.5f - 0.05f)), Vector3.up, _heightOriginal, GetLayerMask()))
            return false;

        _isCrouching = false;
        _targetHeight = _heightOriginal;
        _targetCameraPos = CameraPos;
        _currentSpeed = WalkSpeed;

#if USE_CINEMACHINE

#else
        _currentSwayIntensity = SwayIntensity;
#endif
        return true;
    }

    private void StartfinishCrouchCoroutine()
    {
        _crouchCoroutine = StartCoroutine(FinishCrouchRoutime());
    }

    private IEnumerator FinishCrouchRoutime()
    {
        while (!FinishCrouch())
        {
            // Debug.Log("Can't stay up!");
            yield return new WaitForSeconds(0.1f);
        }
        _crouchCoroutine = null;
    }

    private int GetLayerMask() => ~(1 << gameObject.layer);
}
