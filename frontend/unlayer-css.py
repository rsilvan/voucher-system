#!/usr/bin/env python3
"""Post-process Tailwind v4 CSS to unwrap @layer blocks for QtWebEngine 5.15 compat."""
import re
import sys

def unlayer(css: str) -> str:
    """Remove @layer wrappers but keep their contents."""
    result = []
    i = 0
    while i < len(css):
        # Check for @layer
        m = re.match(r'@layer\s+(\w+(?:,\s*\w+)*)\s*\{', css[i:])
        if m:
            # This is @layer name[, name2, ...] { ... }
            layer_decl = m.group(1)  # e.g., "theme" or "theme, base, components, utilities"
            brace_start = i + m.end() - 1  # position of '{'
            depth = 1
            j = brace_start + 1
            while j < len(css) and depth > 0:
                if css[j] == '{':
                    depth += 1
                elif css[j] == '}':
                    depth -= 1
                j += 1
            # content inside the braces
            inner = css[brace_start+1:j-1]
            result.append(inner)
            i = j
            continue
        
        # Check for @layer name; (empty layer declaration)
        m2 = re.match(r'@layer\s+[\w,\s]+\s*;', css[i:])
        if m2:
            # Skip empty layer declarations
            i += m2.end()
            continue
        
        result.append(css[i])
        i += 1
    
    out = ''.join(result)
    # Clean up: remove double semicolons, excess whitespace
    out = re.sub(r';;+', ';', out)
    out = re.sub(r'\s*\n\s*', '', out)
    return out

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: unlayer-css.py <input.css> [output.css]", file=sys.stderr)
        sys.exit(1)
    
    input_path = sys.argv[1]
    output_path = sys.argv[2] if len(sys.argv) > 2 else input_path
    
    with open(input_path, 'r') as f:
        css = f.read()
    
    result = unlayer(css)
    
    with open(output_path, 'w') as f:
        f.write(result)
    
    print(f"Processed {input_path} -> {output_path}")
    print(f"Original size: {len(css)} bytes")
    print(f"Result size: {len(result)} bytes")
