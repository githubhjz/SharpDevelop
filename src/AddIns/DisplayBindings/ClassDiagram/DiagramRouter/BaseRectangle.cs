// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Itai Bar-Haim" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;

namespace Tools.Diagrams
{
	public abstract class BaseRectangle : IRectangle
	{
		private float x, y;
		private float w = float.NaN, h = float.NaN;
		private float b, p;
		private float aw = float.NaN, ah = float.NaN;
		private bool ar = false;
		
		private IRectangle container;
		
		public IRectangle Container 
		{
			get { return container; }
			set { container = value; }
		}
			
		#region Geometry
		
		public virtual float X
		{
			get { return x; }
			set { x = value; }
		}
	
		public virtual float Y
		{
			get { return y; }
			set { y = value; }
		}
		
		public virtual float AbsoluteX
		{
			get
			{
				if (container != null)
					return container.AbsoluteX + X;
				else
					return X;
			}
		}
	
		public virtual float AbsoluteY
		{
			get
			{
				if (container != null)
					return container.AbsoluteY + Y;
				else
					return Y;
			}
		}
		
		public virtual float ActualWidth
		{
			get { return aw; }
			set
			{
				aw = value;
				if (ar)
					ah = aw * (GetAbsoluteContentHeight() / GetAbsoluteContentWidth());
				OnActualSizeChanged();
				OnActualWidthChanged();
			}
		}
		
		public virtual float ActualHeight
		{
			get { return ah; }
			set
			{
				ah = value;
				if (ar)
					aw = ah * (GetAbsoluteContentWidth() / GetAbsoluteContentHeight());
				OnActualSizeChanged();
				OnActualHeightChanged();
			}
		}
		
		public virtual float Width
		{
			get { return w; }
			set
			{
				w = value;
				OnSizeChanged();
				OnWidthChanged();
			}
		}

		public virtual float Height
		{
			get { return h; }
			set
			{
				h = value;
				OnSizeChanged();
				OnHeightChanged();
			}
		}
				
		#endregion
		
		public virtual float Border
		{
			get { return b; }
			set { b = value; }
		}
				
		public virtual float Padding
		{
			get { return p; }
			set { p = value; }
		}
		
		public virtual float GetAbsoluteContentWidth()
		{
			if (float.IsNaN(w) || w < 0)
				return 0;
			return w;
		}
		
		public virtual float GetAbsoluteContentHeight()
		{
			if (float.IsNaN(h) || h < 0)
				return 0;
			return h;
		}
		
		public bool KeepAspectRatio
		{
			get { return ar; }
			set { ar = value; }
		}
		
		protected virtual void OnSizeChanged() {}
		protected virtual void OnWidthChanged()
		{
			WidthChanged(this, EventArgs.Empty);
		}
		
		protected virtual void OnHeightChanged()
		{
			HeightChanged(this, EventArgs.Empty);
		}
		
		protected virtual void OnActualSizeChanged() {}
	
		protected virtual void OnActualWidthChanged()
		{
			ActualWidthChanged(this, EventArgs.Empty);
		}
			
		protected virtual void OnActualHeightChanged()
		{
			ActualHeightChanged(this, EventArgs.Empty);
		}
		
		public virtual bool IsHResizable
		{
			get { return true; }
		}
		
		public virtual bool IsVResizable
		{
			get { return true; }
		}
		
		public event EventHandler WidthChanged = delegate {};
		public event EventHandler HeightChanged = delegate {};
		public event EventHandler ActualWidthChanged = delegate {};
		public event EventHandler ActualHeightChanged = delegate {};
	}
}
