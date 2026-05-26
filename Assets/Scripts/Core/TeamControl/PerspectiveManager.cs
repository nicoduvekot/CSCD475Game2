using System;
using Units;
using UnityEngine;

namespace TeamControl
{
    public class PerspectiveManager : MonoBehaviour
    {
        public static PerspectiveManager Instance { get; private set; }

        public Perspective CurrentPerspective { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SetPerspective(Perspective perspective)
        {
            CurrentPerspective = perspective;
            OnPerspectiveChanged?.Invoke(perspective);
        }

        public event Action<Perspective> OnPerspectiveChanged;
        
        public static UnitOwner ToOwner(Perspective p)
        {
            return p switch
            {
                Perspective.Player => UnitOwner.Player,
                Perspective.Enemy  => UnitOwner.Enemy,
                Perspective.World  => UnitOwner.World,
                _ => UnitOwner.World // Admin has no owner
            };
        }
    }

    public enum Perspective
    {
        Player,
        Enemy,
        World,
        Admin
    } 
}