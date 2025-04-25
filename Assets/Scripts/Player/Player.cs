using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Input;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(CharacterController2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    
    public STATE _currentState;
    public Platform CurrentPlatform { get; set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _controller = GetComponent<ICharacterController2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }


    void Start()
    {
        _controller.SubscribeToJumpEvent(OnJumpStart);
        _controller.SubscribeToJumpEndEvent(OnJumpEnd);
        
        _controller.SubscribeToJumpDownEvent(OnJumpDown);
    }

    private void OnJumpDown()
    {
        if (CurrentPlatform != null)
        {
            _collider.enabled = false;
            StartCoroutine(Reactivate());
            CurrentPlatform = null;
        }
    }

    private IEnumerator Reactivate()
    {
        yield return new WaitForSeconds(.5f);
        _collider.enabled = true;
    }

    private void OnDestroy()
    {
        _controller.UnsubscribeToJumpEvent(OnJumpStart);
        _controller.UnsubscribeToJumpEndEvent(OnJumpEnd);
    }

    private void FixedUpdate()
    {

        
        switch (_currentState)
        {
            case STATE.MOVE:
                _nbDoubleJump = 0;
                _rigidbody2D.linearVelocity = new Vector2(_xMovement*_currentSpeed*Time.fixedDeltaTime, _rigidbody2D.linearVelocityY);
                break;
            case STATE.JUMP:
                
                LimitJumpHeight(); 
                AirborneMove();

                
               
                break;
            case STATE.FALL:
                _rigidbody2D.AddForce(new Vector2(_controller.MoveInput.x*_jumpMoveForce,0));
                if (Mathf.Abs(_rigidbody2D.linearVelocity.x) > _linearVelocityXOnJumpStart*_jumpVelocityXModifier)
                {
                    float sign = Mathf.Sign(_rigidbody2D.linearVelocity.x);
                    _rigidbody2D.linearVelocity = new Vector2(sign*_linearVelocityXOnJumpStart*_jumpVelocityXModifier,_rigidbody2D.linearVelocity.y);
                }
                break;
            case STATE.HANG:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (Mathf.Abs(_rigidbody2D.linearVelocity.x) > _maxVelocity)
        {
            float sign = Mathf.Sign(_rigidbody2D.linearVelocity.x);
            _rigidbody2D.linearVelocity = new Vector2(sign*_maxVelocity,_rigidbody2D.linearVelocity.y);
        }

        _debugText.SetText(_rigidbody2D.linearVelocity.x.ToString(CultureInfo.InvariantCulture));
    }

    private void LimitJumpHeight()
    {
        if (_rigidbody2D.linearVelocity.y > 0 && _isApplyingJump == false)
        {
            var velocity = _rigidbody2D.linearVelocity;
            velocity += Vector2.up * (Physics2D.gravity.y * _lowJumpFactor * Time.deltaTime);
            _rigidbody2D.linearVelocity = velocity;
        }
    }

    private void AirborneMove()
    {
        _rigidbody2D.AddForce(new Vector2(_xMovement*_jumpMoveForce,0));
        if (Mathf.Abs(_rigidbody2D.linearVelocity.x) > _linearVelocityXOnJumpStart*_jumpVelocityXModifier)
        {
            float sign = Mathf.Sign(_rigidbody2D.linearVelocity.x);
            _rigidbody2D.linearVelocity = new Vector2(sign*_linearVelocityXOnJumpStart*_jumpVelocityXModifier,_rigidbody2D.linearVelocity.y);
        }
    }

    private void GoToFallState()
    {
        _currentState = STATE.FALL;
        _linearVelocityXOnJumpStart = Mathf.Abs(_rigidbody2D.linearVelocity.x);
        
    }

    private bool CanMoveLeft()
    {

        return !Physics2D.OverlapCapsule(_leftCollisionCheck.position, _capsuleSize, CapsuleDirection2D.Vertical, 0,
            m_groundLayerMask);
    }
    
    private bool CanMoveRight()
    {
        return !Physics2D.OverlapCapsule(_rightCollisionCheck.position, _capsuleSize, CapsuleDirection2D.Vertical,0,
            m_groundLayerMask);
    }

    private void OnJumpEnd()
    {
        _isApplyingJump = false;
    }

    private void OnJumpStart()
    {
        _jumpBuffer = 0;
        _isApplyingJump = true;
        if (_isGrounded || _nbDoubleJump <= _maxAirJump)
        {
            Jump();
        }
    }

    private void Jump()
    {
        switch (_currentState)
        {
            case STATE.HANG:
                break;
            default:
                _currentState = STATE.JUMP;
                ResetVerticalVelocity();
                _nbDoubleJump++;
                _isApplyingJump = true;
                _linearVelocityXOnJumpStart = Mathf.Max(1,Mathf.Abs(_rigidbody2D.linearVelocity.x));
                _rigidbody2D.AddForce(Vector2.up*_jumpForce, ForceMode2D.Impulse);
                break;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        _xMovement = Mathf.SmoothDamp(_xMovement,_controller.MoveInput.x,ref _currentVelocity,_smoothTime);
        if (_xMovement < 0 && !CanMoveLeft()) _xMovement = 0;
        if (_xMovement > 0 && !CanMoveRight()) _xMovement = 0;
        
        _jumpBuffer+=Time.deltaTime;
        _isGrounded = Physics2D.OverlapCircle(m_overlapBotton.position, m_groundCheckRadius, m_groundLayerMask);

        switch (_currentState)
        {
            case STATE.MOVE:
                if (_isGrounded == false && _wasGroundedPreviousFrame) GoToFallState();
                break;
            case STATE.JUMP:
                
                if (_rigidbody2D.linearVelocity.y < 0) GoToFallState();
                
                break;
            case STATE.FALL:
                if (_isGrounded && !_wasGroundedPreviousFrame)
                {
                    if (_isApplyingJump && _jumpBuffer < _jumpBufferMax) GoToJumpState();
                    else GoToMoveState();
                }
                break;
            case STATE.HANG:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        _wasGroundedPreviousFrame = _isGrounded;
    }

    private void GoToJumpState()
    {
        _nbDoubleJump = 0;
        ResetVerticalVelocity();
        Jump();
    }

    private void GoToMoveState()
    {
        _currentState = STATE.MOVE;
    }

    private void ResetVerticalVelocity()
    {
        var currentVelocity = _rigidbody2D.linearVelocity;
        currentVelocity.y = 0;
        _rigidbody2D.linearVelocity = currentVelocity;
    }
    #region Privates Variables

    ICharacterController2D _controller;
    Rigidbody2D _rigidbody2D;
    
    [SerializeField] private float _jumpForce=10;
    [SerializeField] private float _currentSpeed =2;
    [SerializeField] private float _lowJumpFactor;
    [FormerlySerializedAs("_isStillJumping")] [SerializeField] private bool _isApplyingJump;
    
    
    private bool _isGrounded;
    
    [Header("Ground Check")]
    [SerializeField] private Transform m_overlapBotton;
    [SerializeField] private float m_groundCheckRadius =.2f;
    [SerializeField] private LayerMask m_groundLayerMask;
    private bool _wasGroundedPreviousFrame;
    
    [Header("Jumping")]
    [SerializeField]private float _jumpMoveForce =5;
    [SerializeField]private float _maxVelocity=200;
    private float _linearVelocityXOnJumpStart;
    [SerializeField] private float _jumpVelocityXModifier;    
    [SerializeField] private int _maxAirJump=1;
    [SerializeField] private float _fallingMultiplier=2.3f;
    
    [SerializeField] private TMP_Text _debugText;
    private float _jumpBuffer;
    [SerializeField] private float _jumpBufferMax =.5f;
    [SerializeField] private Transform _leftCollisionCheck;
    [SerializeField] private Transform _rightCollisionCheck;

    [SerializeField] private Vector2 _capsuleSize = new Vector2(.5f,.5f);
    [SerializeField] private int _nbDoubleJump;
    [SerializeField] private float _smoothTime=.5f;
    private float _xMovement;
    private float _currentVelocity;
    [SerializeField] private Collider2D _collider;

    #endregion


   
}

public enum STATE
{
    MOVE,
    JUMP,
    FALL,
    HANG
}
