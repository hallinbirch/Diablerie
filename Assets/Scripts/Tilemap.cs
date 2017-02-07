﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tilemap : MonoBehaviour {

    static private Tilemap instance;

    public struct Cell
    {
        public bool passable;
        public GameObject gameObject;
    }

    private int width = 1024;
    private int height = 1024;
    private int origin;
    private Cell[] map;

    void Awake() {
        map = new Cell[width * height];
        origin = map.Length / 2;
        instance = this;
        for (int i = 0; i < map.Length; ++i)
            map[i].passable = true;
    }

    class TileOrderComparer : IComparer<Tile> {
        public int Compare(Tile a, Tile b) {
            bool floor1 = a.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            bool floor2 = b.GetComponent<SpriteRenderer>().sortingLayerName == "Floor";
            return -floor1.CompareTo(floor2);
        }
    }

    void Start() {
        Tile[] tiles = GameObject.FindObjectsOfType<Tile>();
        //Array.Sort(tiles, new TileOrderComparer());
        foreach (Tile tile in tiles) {
            if (tile.passable)
                continue;
            Vector3 pos = Iso.MapToIso(tile.transform.position);
            pos.x -= tile.width / 2;
            pos.y -= tile.height / 2;
            pos.x += tile.offsetX;
            pos.y += tile.offsetY;
            for (int x = 0; x < tile.width; ++x) {
                for (int y = 0; y < tile.height; ++y) {
                    int index = MapToIndex(pos + new Vector3(x, y));
                    map[index].passable = tile.passable;
                    map[index].gameObject = tile.gameObject;
                }
            }
        }
    }

    void Update() {
        Color color = new Color(1, 0, 0, 0.3f);
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        int debugWidth = 100;
        int debugHeight = 100;
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;
        int index = instance.MapToIndex(Iso.Snap(pos));
        for (int y = 0; y < debugHeight; ++y)
        {
            for (int x = 0; x < debugWidth; ++x)
            {
                if (!instance.map[index + x].passable)
                    Iso.DebugDrawTile(pos + new Vector3(x, y), color, 0.9f);
            }
            index += width;
        }
    }

    private int MapToIndex(Vector3 tilePos) {
		return origin + Mathf.RoundToInt(tilePos.x + tilePos.y * width);
	}

    public static Cell GetCell(Vector3 pos)
    {
        var tilePos = Iso.Snap(pos);
        int index = instance.MapToIndex(tilePos);
        return instance.map[index];
    }

    public static void SetCell(Vector3 pos, Cell cell)
    {
        var tilePos = Iso.Snap(pos);
        int index = instance.MapToIndex(tilePos);
        instance.map[index] = cell;
    }

    public static bool Passable(Vector3 pos, int radius = 0, bool debug = false)
    {
        var tilePos = Iso.Snap(pos);
        int index = instance.MapToIndex(tilePos);
        bool passable = instance.map[index].passable;
        if (radius == 0)
            return passable;

        passable = passable && instance.map[index - 1].passable;
        passable = passable && instance.map[index + 1].passable;
        passable = passable && instance.map[index - instance.width].passable;
        passable = passable && instance.map[index + instance.width].passable;

        if (debug)
        {
            Iso.DebugDrawTile(tilePos, 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(1, 0), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(-1, 0), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(0, 1), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(0, -1), 0.1f);
        }
        return passable;
    }

    public static bool PassableTile(Vector3 tilePos)
    {
        int index = instance.MapToIndex(tilePos);
        return instance.map[index].passable;
    }

    public static void SetPassable(Vector3 tilePos, bool passable)
    {
        int index = instance.MapToIndex(tilePos);
        instance.map[index].passable = passable;
    }

    public struct RaycastHit
    {
        public bool hit;
        public GameObject gameObject;
        public Vector2 pos;

        public static implicit operator bool(RaycastHit value)
        {
            return value.hit;
        }
    }

    static public RaycastHit Raycast(Vector2 from, Vector2 to, float rayLength = Mathf.Infinity, float maxRayLength = Mathf.Infinity, GameObject ignore = null, bool debug = false)
    {
        var hit = new RaycastHit();
        var diff = to - from;
        var stepLen = 0.2f;
        if (rayLength == Mathf.Infinity)
            rayLength = Mathf.Min(diff.magnitude, maxRayLength);
        int stepCount = Mathf.RoundToInt(rayLength / stepLen);
        var step = diff.normalized * stepLen;
        var pos = from;
        for (int i = 0; i < stepCount; ++i)
        {
            pos += step;
            if (debug)
                Iso.DebugDrawTile(Iso.Snap(pos), margin: 0.3f, duration: 0.5f);
            Cell cell = GetCell(pos);
            bool passable = Passable(pos, 2);
            if (!passable && (ignore == null || ignore != cell.gameObject))
            {
                hit.hit = !passable;
                hit.gameObject = cell.gameObject;
                break;
            }
        }
        return hit;
    }

    static public int OverlapBox(Vector2 center, Vector2 size, GameObject[] result)
    {
        int count = 0;
        if (result.Length == 0)
            return 0;
        int rows = Mathf.RoundToInt(size.y);
        int columns = Mathf.RoundToInt(size.x);
        int index = instance.MapToIndex(Iso.Snap(center - size / 2));
        for(int row = 0; row < rows; ++row)
        {
            for(int column = 0; column < columns; ++column)
            {
                var gameObject = instance.map[index + column].gameObject;
                if (gameObject != null)
                {
                    result[count] = gameObject;
                    count += 1;
                    if (count >= result.Length)
                        return count;
                }
            }
            index += instance.width;
        }
        return count;
    }

    void OnDrawGizmos()
    {
        var cameraTile = Iso.MacroTile(Iso.MapToIso(Camera.current.transform.position));
        Gizmos.color = new Color(0.35f, 0.35f, 0.35f);
        for (int x = -10; x < 10; ++x)
        {
            var pos = Iso.MapToWorld(cameraTile + new Vector3(x, 10) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, -10f));
        }

        for (int y = -10; y < 10; ++y)
        {
            var pos = Iso.MapToWorld(cameraTile + new Vector3(-10, y) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, 10f));
        }
    }
}
