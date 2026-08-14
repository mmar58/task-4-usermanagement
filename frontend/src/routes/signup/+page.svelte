<script lang="ts">
    import { Button } from '$lib/components/ui/button';
    import { Input } from '$lib/components/ui/input';
    import { Label } from '$lib/components/ui/label';
    import { api } from '$lib/api';
    import { goto } from '$app/navigation';
    
    let name = $state('');
    let email = $state('');
    let password = $state('');
    let errorMessage = $state('');
    let successMessage = $state('');
    let isLoading = $state(false);

    async function handleSignup(e: Event) {
        e.preventDefault();
        isLoading = true;
        errorMessage = '';
        successMessage = '';

        try {
            const response = await api.post('/auth/register', { name, email, password });
            successMessage = response.data.message || 'Registration successful. Redirecting to login...';
            setTimeout(() => {
                goto('/login');
            }, 3000);
        } catch (error: any) {
            errorMessage = error.response?.data?.message || 'Failed to register. Please try again.';
        } finally {
            isLoading = false;
        }
    }
</script>

<div class="min-h-screen flex items-center justify-center bg-white p-4">
    <div class="w-full max-w-sm space-y-8">
        <div class="flex items-center space-x-2">
            <span class="text-blue-600 font-bold text-xl tracking-[0.2em] font-sans">THE APP</span>
        </div>

        <div class="pt-8 space-y-2">
            <p class="text-sm text-gray-500">Join us today</p>
            <h1 class="text-2xl font-semibold tracking-tight text-gray-900">Sign Up to The App</h1>
        </div>

        {#if errorMessage}
            <div class="bg-red-50 text-red-500 p-3 rounded text-sm">
                {errorMessage}
            </div>
        {/if}
        
        {#if successMessage}
            <div class="bg-green-50 text-green-600 p-3 rounded text-sm">
                {successMessage}
            </div>
        {/if}

        <form class="space-y-6" onsubmit={handleSignup}>
            <div class="space-y-2">
                <Label class="text-gray-500 text-xs font-normal" for="name">Name</Label>
                <div class="relative">
                    <Input id="name" type="text" placeholder="John Doe" bind:value={name} required class="h-12 border-gray-200 bg-white placeholder:text-gray-800" />
                </div>
            </div>

            <div class="space-y-2">
                <Label class="text-gray-500 text-xs font-normal" for="email">E-mail</Label>
                <div class="relative">
                    <Input id="email" type="email" placeholder="test@example.com" bind:value={email} required class="h-12 border-gray-200 bg-white placeholder:text-gray-800" />
                </div>
            </div>

            <div class="space-y-2">
                <Label class="text-gray-500 text-xs font-normal" for="password">Password</Label>
                <div class="relative">
                    <Input id="password" type="password" placeholder="••••••••" bind:value={password} required class="h-12 border-gray-200 bg-white placeholder:text-gray-800" />
                </div>
            </div>

            <Button type="submit" class="w-full bg-blue-600 hover:bg-blue-700 text-white h-12 text-md" disabled={isLoading}>
                {isLoading ? 'Signing Up...' : 'Sign Up'}
            </Button>
        </form>

        <div class="flex justify-between items-center text-sm pt-8">
            <span class="text-gray-500">Already have an account? <a href="/login" class="text-blue-500 hover:underline">Sign in</a></span>
        </div>
    </div>
</div>
