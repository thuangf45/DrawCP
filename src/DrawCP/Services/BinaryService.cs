using DrawCP.Models;

namespace DrawCP.Services;

public static class BinaryService
{
    public static void Save(string path, List<ShapeModel> shapes)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write(shapes.Count);
        foreach (var s in shapes)
        {
            writer.Write((int)s.Type);
            writer.Write(s.X);
            writer.Write(s.Y);
            writer.Write(s.Width);
            writer.Write(s.Height);

            // Lưu mã màu dưới dạng chuỗi ARGB đầy đủ
            writer.Write(s.StrokeColor.ToArgbHex());
            writer.Write(s.FillColor.ToArgbHex());

            writer.Write(s.StrokeThickness);
        }
    }

    public static List<ShapeModel> Load(string path)
    {
        var list = new List<ShapeModel>();
        if (!File.Exists(path)) return list;

        using var reader = new BinaryReader(File.Open(path, FileMode.Open));
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            list.Add(new ShapeModel
            {
                Type = (MyShapeType)reader.ReadInt32(),
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Width = reader.ReadSingle(),
                Height = reader.ReadSingle(),
                // Dùng FromString để nhận diện mã Hex chuẩn xác
                StrokeColor = Color.FromArgb(reader.ReadString()),
                FillColor = Color.FromArgb(reader.ReadString()),
                StrokeThickness = reader.ReadSingle()
            });
        }
        return list;
    }
}