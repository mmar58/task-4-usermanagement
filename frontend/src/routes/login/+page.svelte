<script lang="ts">
    import { Button } from "$lib/components/ui/button";
    import { Input } from "$lib/components/ui/input";
    import { Label } from "$lib/components/ui/label";
    import { Checkbox } from "$lib/components/ui/checkbox";
    import { api } from "$lib/api";
    import { goto } from "$app/navigation";
    import { page } from "$app/state";

    let email = $state("");
    let password = $state("");
    let rememberMe = $state(false);
    let errorMessage = $state(page.url.searchParams.get("error") || "");
    let isLoading = $state(false);

    async function handleLogin(e: Event) {
        e.preventDefault();
        isLoading = true;
        errorMessage = "";

        try {
            const response = await api.post("/auth/login", { email, password });
            const { token } = response.data;

            localStorage.setItem("token", token);

            // Redirect to dashboard
            goto("/");
        } catch (error: any) {
            errorMessage =
                error.response?.data?.message ||
                "Failed to login. Please try again.";
        } finally {
            isLoading = false;
        }
    }
</script>

<div class="min-h-screen flex items-center justify-center bg-white p-4">
    <div class="w-full max-w-sm space-y-8">
        <div class="flex items-center space-x-2">
            <span
                class="text-blue-600 font-bold text-xl tracking-[0.2em] font-sans"
                >THE APP</span
            >
        </div>

        <div class="pt-8 space-y-2">
            <p class="text-sm text-gray-500">Start your journey</p>
            <h1 class="text-2xl font-semibold tracking-tight text-gray-900">
                Sign In to The App
            </h1>
        </div>

        {#if errorMessage}
            <div class="bg-red-50 text-red-500 p-3 rounded text-sm">
                {errorMessage}
            </div>
        {/if}

        <form class="space-y-6" onsubmit={handleLogin}>
            <div class="space-y-2">
                <Label class="text-gray-500 text-xs font-normal" for="email"
                    >E-mail</Label
                >
                <div class="relative">
                    <Input
                        id="email"
                        type="email"
                        placeholder=""
                        bind:value={email}
                        required
                        class="h-12 border-gray-200 bg-white placeholder:text-gray-500"
                    />
                    <!-- Using text placeholder for mail icon to save an import if possible, but lucide is installed -->
                    <span class="absolute right-3 top-3.5 text-gray-400">
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="18"
                            height="18"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            ><rect
                                width="20"
                                height="16"
                                x="2"
                                y="4"
                                rx="2"
                            /><path
                                d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"
                            /></svg
                        >
                    </span>
                </div>
            </div>

            <div class="space-y-2">
                <Label class="text-gray-500 text-xs font-normal" for="password"
                    >Password</Label
                >
                <div class="relative">
                    <Input
                        id="password"
                        type="password"
                        placeholder=""
                        bind:value={password}
                        class="h-12 border-gray-200 bg-white placeholder:text-gray-800"
                    />
                    <span class="absolute right-3 top-3.5 text-gray-400">
                        <svg
                            xmlns="http://www.w3.org/2000/svg"
                            width="18"
                            height="18"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            ><path
                                d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"
                            /><circle cx="12" cy="12" r="3" /></svg
                        >
                    </span>
                </div>
            </div>

            <div class="flex items-center space-x-2">
                <Checkbox
                    id="remember"
                    bind:checked={rememberMe}
                    class="border-gray-300 data-[state=checked]:bg-blue-600"
                />
                <label
                    for="remember"
                    class="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 text-gray-700"
                >
                    Remember me
                </label>
            </div>

            <Button
                type="submit"
                class="w-full bg-blue-600 hover:bg-blue-700 text-white h-12 text-md"
                disabled={isLoading}
            >
                {isLoading ? "Signing In..." : "Sign In"}
            </Button>
        </form>

        <div class="flex justify-between items-center text-sm pt-8">
            <span class="text-gray-500"
                >Don't have an account? <a
                    href="/signup"
                    class="text-blue-500 hover:underline">Sign up</a
                ></span
            >
            <a href="#" class="text-blue-500 hover:underline"
                >Forgot password?</a
            >
        </div>
    </div>
</div>
