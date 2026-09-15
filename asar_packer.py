# -*- coding: utf-8 -*-
"""
Pure Python Electron ASAR Packer and Unpacker.
Strictly adheres to Chromium pickle / Electron ASAR specification.
Zero external dependencies (no 7-Zip, no Node.js required).
"""
import os
import sys
import json
import struct
import hashlib

BLOCK_SIZE = 4 * 1024 * 1024  # 4 MB integrity blocks

def calc_file_integrity(file_path):
    """Calculates SHA256 integrity object with 4MB blocks matching Electron format."""
    file_size = os.path.getsize(file_path)
    if file_size == 0:
        return {
            "algorithm": "SHA256",
            "hash": hashlib.sha256(b"").hexdigest(),
            "blockSize": BLOCK_SIZE,
            "blocks": [hashlib.sha256(b"").hexdigest()]
        }
    
    blocks = []
    hasher = hashlib.sha256()
    with open(file_path, "rb") as f:
        while True:
            chunk = f.read(BLOCK_SIZE)
            if not chunk:
                break
            blocks.append(hashlib.sha256(chunk).hexdigest())
            hasher.update(chunk)
            
    return {
        "algorithm": "SHA256",
        "hash": hasher.hexdigest(),
        "blockSize": BLOCK_SIZE,
        "blocks": blocks
    }

def pack_asar(src_dir, output_asar_path, unpacked_dest_dir=None, unpacked_patterns=None):
    """
    Packs a directory into an ASAR archive.
    Files matching unpacked_patterns are marked unpacked: true and written to unpacked_dest_dir.
    """
    if unpacked_patterns is None:
        unpacked_patterns = ["node_modules/chrome-devtools-mcp", "node_modules\\chrome-devtools-mcp"]

    def is_unpacked(rel_path):
        norm = rel_path.replace("\\", "/")
        for p in unpacked_patterns:
            if norm.startswith(p.replace("\\", "/")):
                return True
        return False

    file_entries = []  # list of (rel_path, abs_path, is_unpacked, size)
    for root, dirs, files in os.walk(src_dir):
        # Sort for deterministic output
        dirs.sort()
        files.sort()
        for f in files:
            abs_path = os.path.join(root, f)
            rel_path = os.path.relpath(abs_path, src_dir).replace("\\", "/")
            size = os.path.getsize(abs_path)
            unpacked = is_unpacked(rel_path)
            file_entries.append((rel_path, abs_path, unpacked, size))

    # Build JSON header tree and calculate offsets
    root_header = {"files": {}}
    current_offset = 0

    for rel_path, abs_path, unpacked, size in file_entries:
        parts = rel_path.split("/")
        node = root_header["files"]
        for p in parts[:-1]:
            if p not in node:
                node[p] = {"files": {}}
            node = node[p]["files"]

        filename = parts[-1]
        integrity = calc_file_integrity(abs_path)

        if unpacked:
            node[filename] = {
                "size": size,
                "unpacked": True,
                "integrity": integrity
            }
        else:
            node[filename] = {
                "size": size,
                "offset": str(current_offset),
                "integrity": integrity
            }
            current_offset += size

    json_str = json.dumps(root_header, separators=(',', ':'), ensure_ascii=False)
    json_bytes = json_str.encode('utf-8')
    json_len = len(json_bytes)

    # 4-byte alignment
    padding_len = (4 - (json_len % 4)) % 4
    header_raw_size = json_len + padding_len + 4
    header_size = header_raw_size + 4

    os.makedirs(os.path.dirname(os.path.abspath(output_asar_path)), exist_ok=True)
    temp_output = output_asar_path + ".tmp"

    with open(temp_output, "wb") as out_f:
        # 16-byte pickle header
        out_f.write(struct.pack('<4I', 4, header_size, header_raw_size, json_len))
        # JSON header string
        out_f.write(json_bytes)
        # Padding
        if padding_len > 0:
            out_f.write(b'\x00' * padding_len)

        # Sequential file payloads
        for rel_path, abs_path, unpacked, size in file_entries:
            if not unpacked:
                with open(abs_path, "rb") as in_f:
                    # Stream in 1MB chunks
                    while True:
                        buf = in_f.read(1024 * 1024)
                        if not buf:
                            break
                        out_f.write(buf)

    if os.path.exists(output_asar_path):
        os.remove(output_asar_path)
    os.rename(temp_output, output_asar_path)
    print(f"[ASAR] Successfully packed {len(file_entries)} files into {output_asar_path} ({os.path.getsize(output_asar_path)} bytes)")

    # Copy unpacked files if destination specified
    if unpacked_dest_dir:
        os.makedirs(unpacked_dest_dir, exist_ok=True)
        unpacked_count = 0
        for rel_path, abs_path, unpacked, size in file_entries:
            if unpacked:
                target_file = os.path.join(unpacked_dest_dir, rel_path.replace("/", os.sep))
                os.makedirs(os.path.dirname(target_file), exist_ok=True)
                with open(abs_path, "rb") as sf, open(target_file, "wb") as df:
                    df.write(sf.read())
                unpacked_count += 1
        print(f"[ASAR] Copied {unpacked_count} unpacked files to {unpacked_dest_dir}")

def unpack_asar(asar_path, output_dir):
    """Unpacks an ASAR archive into output_dir."""
    with open(asar_path, "rb") as f:
        magic, header_size, header_raw_size, json_len = struct.unpack('<4I', f.read(16))
        json_bytes = f.read(json_len)
        header = json.loads(json_bytes.decode('utf-8'))
        
        # Calculate data offset
        padding_len = (4 - (json_len % 4)) % 4
        data_base_offset = 16 + json_len + padding_len

        def extract_node(node, current_path):
            if "files" in node:
                for name, child in node["files"].items():
                    extract_node(child, os.path.join(current_path, name))
            else:
                if node.get("unpacked"):
                    return
                offset = int(node["offset"])
                size = node["size"]
                target_path = current_path
                os.makedirs(os.path.dirname(target_path), exist_ok=True)
                f.seek(data_base_offset + offset)
                content = f.read(size)
                with open(target_path, "wb") as out:
                    out.write(content)

        extract_node(header, output_dir)
        print(f"[ASAR] Successfully extracted {asar_path} to {output_dir}")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage:")
        print("  python asar_packer.py pack <source_dir> <output.asar> [unpacked_dir]")
        print("  python asar_packer.py unpack <input.asar> <output_dir>")
        sys.exit(1)

    cmd = sys.argv[1].lower()
    if cmd == "pack":
        src = sys.argv[2]
        out = sys.argv[3]
        unpacked = sys.argv[4] if len(sys.argv) > 4 else None
        pack_asar(src, out, unpacked)
    elif cmd == "unpack":
        inp = sys.argv[2]
        out = sys.argv[3]
        unpack_asar(inp, out)
    else:
        print(f"Unknown command: {cmd}")
        sys.exit(1)
