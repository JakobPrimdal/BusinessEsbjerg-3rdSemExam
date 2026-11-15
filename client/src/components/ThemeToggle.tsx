import { useEffect, useState } from "react";
import { Moon, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";

const KEY = "theme";

function initialDark(): boolean {
  try {
    const saved = localStorage.getItem(KEY);
    if (saved) return saved === "dark";
  } catch {
    // storage blocked: fall back to the OS setting
  }
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

export function ThemeToggle() {
  const [dark, setDark] = useState(initialDark);

  useEffect(() => {
    document.documentElement.classList.toggle("dark", dark);
    try {
      localStorage.setItem(KEY, dark ? "dark" : "light");
    } catch {
      // ignore
    }
  }, [dark]);

  return (
    <Button
      variant="outline"
      size="icon"
      onClick={() => setDark(d => !d)}
      aria-label={dark ? "Skift til lyst tema" : "Skift til mørkt tema"}
    >
      {dark ? <Sun /> : <Moon />}
    </Button>
  );
}
