using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using APPLIB;

namespace LZS_unpack
{
	/// <summary>
	/// Parser for .mesh.ascii files
	/// </summary>
	internal class MeshAsciiParser
	{
		public class Submesh
		{
			public string Name;
			public string MaterialName;
			public List<Vector3D> Vertices = new List<Vector3D>();
			public List<int[]> Faces = new List<int[]>();
		}

		public List<Submesh> Submeshes = new List<Submesh>();

		public void Parse(string filePath)
		{
			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";

			StreamReader sr = new StreamReader(filePath);
			string line;

			// Skip first line (version)
			sr.ReadLine();

			// Read total vertex count (we'll recalculate per submesh)
			sr.ReadLine();

			while ((line = sr.ReadLine()) != null)
			{
				line = line.Trim();
				if (line.StartsWith("Submesh"))
				{
					ParseSubmesh(ref sr, line, nfi);
				}
			}
			sr.Close();
		}

		private void ParseSubmesh(ref StreamReader sr, string submeshLine, NumberFormatInfo nfi)
		{
			Submesh submesh = new Submesh();
			submesh.Name = submeshLine;

			// Skip 3 lines (format info)
			sr.ReadLine(); // "1"
			sr.ReadLine(); // "1"
			submesh.MaterialName = sr.ReadLine(); // material name
			sr.ReadLine(); // "0"

			// Read vertex count
			string vertexCountLine = sr.ReadLine();
			int vertexCount = int.Parse(vertexCountLine);

			// Read vertices
			for (int i = 0; i < vertexCount; i++)
			{
				string vLine = sr.ReadLine();
				if (vLine == null) break;

				string[] parts = vLine.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 3)
				{
					Vector3D v = new Vector3D(
						float.Parse(parts[0], nfi),
						float.Parse(parts[1], nfi),
						float.Parse(parts[2], nfi)
					);
					submesh.Vertices.Add(v);
				}

				// Skip next 3 lines (normal, color, UV)
				sr.ReadLine();
				sr.ReadLine();
				sr.ReadLine();
			}

			// Read face count
			string faceCountLine = sr.ReadLine();
			int faceCount = int.Parse(faceCountLine);

			// Read faces
			for (int i = 0; i < faceCount; i++)
			{
				string fLine = sr.ReadLine();
				if (fLine == null) break;

				string[] parts = fLine.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 3)
				{
					int[] face = new int[3];
					face[0] = int.Parse(parts[0]);
					face[1] = int.Parse(parts[1]);
					face[2] = int.Parse(parts[2]);
					submesh.Faces.Add(face);
				}
			}

			Submeshes.Add(submesh);
		}
	}
}

