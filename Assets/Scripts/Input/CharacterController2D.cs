using System;
using com.ajc.input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input
{
    public interface ICharacterController2D
    {
        void SubscribeToJumpEvent(Action jumpAction);
        void SubscribeToJumpEndEvent(Action jumpAction);
        void UnsubscribeToJumpEvent(Action jumpAction);
        void UnsubscribeToJumpEndEvent(Action jumpAction);
        
        void SubscribeToJumpDownEvent(Action jumpAction);
        void UnsubscribeToJumpDownEvent(Action jumpAction);
        Vector2 MoveInput { get; }
    }

    public class CharacterController2D : MonoBehaviour, GameInputSystem.IPlayerActions, ICharacterController2D
    {


        public Vector2 MoveInput => _moveInput;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _gameInputSystem = new GameInputSystem();
            _gameInputSystem.Enable();
            _gameInputSystem.Player.SetCallbacks(this);
        }

        private void OnDisable()
        {
            _gameInputSystem.Disable();
        }

        // Update is called once per frame
        void Update()
        {
            
            _moveInput = _gameInputSystem.Player.Move.ReadValue<Vector2>();
            
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if(context.started) _onJumpStartEvent?.Invoke();
            if(context.canceled) _onJumpEndEvent?.Invoke();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            
        }

        public void OnJumpDown(InputAction.CallbackContext context)
        {
            if(context.started) _onJumpDownEvent?.Invoke();
        }

        public void SubscribeToJumpEvent(Action jumpAction)
        {
            _onJumpStartEvent += jumpAction;
        }
        public void SubscribeToJumpEndEvent(Action jumpAction)
        {
            _onJumpEndEvent += jumpAction;
        }
        public void UnsubscribeToJumpEvent(Action jumpAction)
        {
            _onJumpStartEvent -= jumpAction;
        }
        
        public void UnsubscribeToJumpEndEvent(Action jumpAction)
        {
            _onJumpEndEvent -= jumpAction;
        }

        public void SubscribeToJumpDownEvent(Action jumpAction)
        {
            _onJumpDownEvent+=jumpAction;
        }
        
        public void UnsubscribeToJumpDownEvent(Action jumpAction)
        {
            _onJumpDownEvent-=jumpAction;
        }

        #region Private Variables

        private Action _onJumpStartEvent;
        private Action _onJumpEndEvent;
        
        private GameInputSystem _gameInputSystem;
        private Vector2 _moveInput;
        private Action _onJumpDownEvent;

        #endregion
    }
    
}
