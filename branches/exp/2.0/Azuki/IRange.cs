namespace Sgry.Azuki
{
	public interface IRange
	{
		int Begin { get; set; }

		int End { get; set; }

		int Length { get; }

		string Text{ get; }

		bool IsEmpty { get; }

		Range Intersect( IRange another );
	}
}
