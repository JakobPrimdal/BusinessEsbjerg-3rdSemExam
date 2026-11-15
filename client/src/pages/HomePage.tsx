import { useAuth } from "@/auth/AuthContext";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

// Placeholder
export function HomePage() {
  const { user } = useAuth();

  return (
    <Card className="w-full max-w-md">
      <CardHeader>
        <CardTitle>Velkommen, {user?.username}</CardTitle>
      </CardHeader>
      <CardContent className="text-muted-foreground">Her kommer din ferieoversigt.</CardContent>
    </Card>
  );
}
