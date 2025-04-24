using System;
using System.Diagnostics;
using System.Globalization;
using Input;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    
    public STATE _currentState;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _controller = GetComponent<ICharacterController2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }


    void Start()
    {
        _controller.SubscribeToJumpEvent(OnJump);
        _controller.SubscribeToJumpEndEvent(OnJumpEnd);
    }

    private void OnDestroy()
    {
        _controller.UnsubscribeToJumpEvent(OnJump);
        _controller.UnsubscribeToJumpEndEvent(OnJumpEnd);
    }

    private void FixedUpdate()
    {
        var xMovement = _controller.MoveInput.x;
        _camMoveRight = CanMoveRight();
        _camMoveLeft = CanMoveLeft();
        if (xMovement < 0 && !CanMoveLeft()) xMovement = 0;
        if (xMovement > 0 && !CanMoveRight()) xMovement = 0;
        
        if (Mathf.Abs(_rigidbody2D.linearVelocity.x) > _maxVelocity)
        {
            float sign = Mathf.Sign(_rigidbody2D.linearVelocity.x);
            _rigidbody2D.linearVelocity = new Vector2(sign*_maxVelocity,_rigidbody2D.linearVelocity.y);
        }

        switch (_currentState)
        {
            case STATE.MOVE:
                _nbDoubleJump = 0;
                _rigidbody2D.linearVelocity = new Vector2(xMovement*_currentSpeed*Time.fixedDeltaTime, _rigidbody2D.linearVelocityY);
                
                break;
            case STATE.JUMP:
                if (_rigidbody2D.linearVelocity.y > 0 && _isApplyingJump == false)
                {
                    var velocity = _rigidbody2D.linearVelocity;
                    velocity += Vector2.up * (Physics2D.gravity.y * _lowJumpFactor * Time.deltaTime);
                    _rigidbody2D.linearVelocity = velocity;
                }
                _rigidbody2D.AddForce(new Vector2(_controller.MoveInput.x*_jumpMoveForce,0));
                if (Mathf.Abs(_rigidbody2D.linearVelocity.x) > _linearVelocityXOnJumpStart*_jumpVelocityXModifier)
                {
                    float sign = Mathf.Sign(_rigidbody2D.linearVelocity.x);
                    _rigidbody2D.linearVelocity = new Vector2(sign*_linearVelocityXOnJumpStart*_jumpVelocityXModifier,_rigidbody2D.linearVelocity.y);
                }
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

    private void GoToFallState()
    {
        _currentState = STATE.FALL;
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

    private void OnJump()
    {
        _jumpBuffer = 0;
        _isApplyingJump = true;
        if (_isGrounded || _nbDoubleJump < 1)
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
                _isJumping = true;
                _isApplyingJump = true;
                _linearVelocityXOnJumpStart = Mathf.Max(1,Mathf.Abs(_rigidbody2D.linearVelocity.x));
                _rigidbody2D.AddForce(Vector2.up*_jumpForce, ForceMode2D.Impulse);
                break;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        _jumpBuffer+=Time.deltaTime;
        _isGrounded = Physics2D.OverlapCircle(m_overlapBotton.position, m_groundCheckRadius, m_groundLayerMask);

        if (_isGrounded && !_wasGroundedPreviousFrame)
        {
            if (_isApplyingJump && _jumpBuffer < _jumpBufferMax) GoToJumpState();
            else GoToMoveState();
        }
        
        if (_isGrounded == false && _wasGroundedPreviousFrame) GoToFallState();
        {
            /*if (_isJumping == false)
            {
                _linearVelocityXOnJumpStart = Mathf.Abs(_rigidbody2D.linearVelocity.x);
            }*/
        }
        
        
        if (_rigidbody2D.linearVelocity.y < 0) GoToFallState();
        _wasGroundedPreviousFrame = _isGrounded;
    }

    private void GoToJumpState()
    {
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
    private bool _isJumping;
    
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
    
    [SerializeField] private TMP_Text _debugText;
    private float _jumpBuffer;
    [SerializeField] private float _jumpBufferMax =.5f;
    [SerializeField] private Transform _leftCollisionCheck;
    [SerializeField] private Transform _rightCollisionCheck;
    private bool _camMoveRight;
    private bool _camMoveLeft;
    [SerializeField] private Vector2 _capsuleSize = new Vector2(.5f,.5f);
    [SerializeField] private int _nbDoubleJump;

    #endregion


   
}

public enum STATE
{
    MOVE,
    JUMP,
    FALL,
    HANG
}
