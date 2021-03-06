﻿using System;
using FBLibrary.Core.BaseEntities;
using Microsoft.Xna.Framework;

namespace FBLibrary.Core
{
    public abstract class BaseEntity : IEntity
    {
        private Vector2 _position;
        private Point _cellPosition;

        public bool IsAlive;
        public bool InDestruction;
        public TimeSpan DestructionTime;

        #region Properties

        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public float PositionX
        {
            set { _position.X = value; }
            get { return _position.X; }
        }

        public float PositionY
        {
            set { _position.Y = value; }
            get { return _position.Y; }
        }

        public int CellPositionX
        {
            get { return _cellPosition.X; }
            set { _cellPosition.X = value; }
        }

        public int CellPositionY
        {
            get { return _cellPosition.Y; }
            set { _cellPosition.Y = value; }
        }

        public Point CellPosition
        {
            get { return _cellPosition; }
            protected set { _cellPosition = value; }
        }

        public Point Dimension { get; set; }

        #endregion

        protected BaseEntity()
        {
            Dimension = GameConfiguration.BaseTileSize;
            IsAlive = true;
            InDestruction = false;
            DestructionTime = TimeSpan.Zero;
        }

        public virtual void Update()
        {
            if (InDestruction)
            {
                DestructionTime -= TimeSpan.FromTicks(GameConfiguration.DeltaTime);

                if (DestructionTime < TimeSpan.Zero)
                    Remove();
            }
        }

        protected BaseEntity(Point cellPosition) : this()
        {
            _cellPosition = cellPosition;
            Position = Engine.CellToVector(CellPosition);
        }

        public void ChangePosition(Point p)
        {
            _cellPosition = p;
            Position = Engine.CellToVector(CellPosition);
        }

        public void ChangePosition(int x, int y)
        {
            _cellPosition.X = x;
            _cellPosition.Y = y;
            Position = Engine.CellToVector(CellPosition);
        }

        public void ChangePosition(float x, float y)
        {
            _position.X = x;
            _position.Y = y;
            _cellPosition = Engine.VectorToCell(Position);
        }

        public abstract void Destroy();
        public abstract void Remove();
    }
}
