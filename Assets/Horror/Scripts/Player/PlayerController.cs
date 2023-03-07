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
    public Camera MainCamera;
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

    [SerializeField]
    private float SwayIntensity = 1.5f;
    [SerializeField]
    private float Amplitude = 0.25f;

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
    private Vector3 _nextSwayVector;
    private Vector3 _nextSwayPosition;
    private Vector2 _mouseDelta;
    private float _jumpVelocity;
    private float _xRotation;
    private float _currentSpeed;
    private float _nextSprintTime;
    private float _currentSprintDuration;
    private float _heightOriginal;
    private float _targetHeight;
    private float _currentSwayIntensity;
    private bool _isSprinting;
    private bool _isCrouching;
    private bool _lastHoldToSprint;
    private bool _lastHoldToCrouch;

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

        if (MainCamera == null)
            MainCamera = Camera.main;

        MainCamera.transform.parent = transform;
        MainCamera.transform.localPosition = CameraPos;
        MainCamera.transform.localRotation = transform.localRotation;
        MainCamera.fieldOfView = FieldOfView;

        _jumpVelocity = JumpPeriod * Mathf.Abs(Physics.gravity.y);
        _currentSpeed = WalkSpeed;
        _currentSprintDuration = SprintDuration;
        _targetHeight = _heightOriginal = _characterController.height;
        _targetCameraPos = CameraPos;

        _nextSwayVector = Amplitude * Vector3.up;
        _nextSwayPosition = _targetCameraPos + _nextSwayVector;
        _currentSwayIntensity = SwayIntensity;
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

    public void TeleportTo(Transform transform_)
    {
        _characterController.enabled = false;
        transform.SetPositionAndRotation(transform_.position, transform_.rotation);
        _characterController.enabled = true;
    }

    private void ApplyCameraSettings()
    {
        if (MainCamera.fieldOfView != FieldOfView)
            MainCamera.fieldOfView = FieldOfView;
    }

    private void CameraSway()
    {
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
        if (Physics.OverlapSphere(spherePos, _characterController.radius, gameObject.layer).Length > 0)
            _verticalVelocity.y = _jumpVelocity;
    }

    private void Rotate(Vector2 delta)
    {
        delta *= MouseSencitivity;
        float invertValX = InvertCameraXRotation ? -1 : 1;
        float invertValY = InvertCameraYRotation ? -1 : 1;

        CamRotate(delta.y * invertValY);
        transform.Rotate(0f, delta.x * invertValX, 0f);
    }

    public void CamRotate(float xAngle)
    {
        _xRotation -= xAngle;
        _xRotation = Mathf.Clamp(_xRotation, MinY, MaxY);
        MainCamera.transform.rotation = Quaternion.Euler(_xRotation, transform.eulerAngles.y, 0);
    }

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
        _currentSwayIntensity *= 1.5f;
    }

    private void FinishSprint()
    {
        if (!_isSprinting) return;
        _isSprinting = false;

        _currentSpeed = WalkSpeed;
        _nextSprintTime = Time.time + SprintCooldown;
        _currentSwayIntensity = SwayIntensity;
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
        _currentSwayIntensity *= 0.5f;
    }

    private bool FinishCrouch()
    {
        if (Physics.Raycast(transform.position - (Vector3.up * (_characterController.height * 0.5f - 0.05f)), Vector3.up, _heightOriginal, gameObject.layer))
            return false;

        _isCrouching = false;
        _targetHeight = _heightOriginal;
        _targetCameraPos = CameraPos;
        _currentSpeed = WalkSpeed;
        _currentSwayIntensity = SwayIntensity;
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
}
